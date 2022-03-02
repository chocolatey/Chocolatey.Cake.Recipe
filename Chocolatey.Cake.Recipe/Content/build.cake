///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var publishingError = false;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildData>(context =>
{
    Information(Figlet(BuildParameters.Title));

    Information("Starting Setup...");

    if (BuildParameters.BranchType == BranchType.Master && (context.Log.Verbosity != Verbosity.Diagnostic)) {
        Information("Increasing verbosity to diagnostic.");
        context.Log.Verbosity = Verbosity.Diagnostic;
    }

    RequireTool(ToolSettings.GitVersionTool, () => {
        BuildParameters.SetBuildVersion(
            BuildVersion.CalculatingSemanticVersion(
                context: Context,
                preReleaseLabelFilePath: BuildParameters.PreReleaseLabelFilePath
            )
        );
    });

    Information("Building version {0} of " + BuildParameters.Title + " ({1}, {2}) using version {3} of Cake, , and version {4} of Chocolatey.Cake.Recipe. (IsTagged: {5})",
        BuildParameters.Version.PackageVersion,
        BuildParameters.Configuration,
        BuildParameters.Target,
        BuildParameters.Version.CakeVersion,
        BuildMetaData.Version,
        BuildParameters.IsTagged);

    return new BuildData(context);
});

Teardown(context =>
{
    Information("Starting Teardown...");

    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

BuildParameters.Tasks.CleanTask = Task("Clean")
    .Does(() =>
{
    Information("Cleaning...");

    CleanDirectories(BuildParameters.Paths.Directories.ToClean);
});

BuildParameters.Tasks.RestoreTask = Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information("Restoring {0}...", BuildParameters.SolutionFilePath);

    NuGetRestore(
        BuildParameters.SolutionFilePath,
        new NuGetRestoreSettings
        {
            Source = BuildParameters.NuGetSources,
            PackagesDirectory = BuildParameters.RestorePackagesDirectory
        });
});

BuildParameters.Tasks.DotNetCoreRestoreTask = Task("DotNetCoreRestore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", BuildParameters.Version.SemVersion)
                            .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                            .WithProperty("FileVersion",  BuildParameters.Version.Version)
                            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

    if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
    {
        var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

        // Use FrameworkPathOverride when not running on Windows.
        Information("Restore will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
        msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
    }

    DotNetCoreRestore(BuildParameters.SolutionFilePath.FullPath, new DotNetCoreRestoreSettings
    {
        Sources = BuildParameters.NuGetSources,
        MSBuildSettings = msBuildSettings,
        PackagesDirectory = BuildParameters.RestorePackagesDirectory
    });
});

BuildParameters.Tasks.BuildTask = Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does<BuildData>(data => RequireTool(ToolSettings.MSBuildExtensionPackTool, () => {
        Information("Building {0}", BuildParameters.SolutionFilePath);

        if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows)
        {
            var msbuildSettings = new MSBuildSettings()
                .SetPlatformTarget(ToolSettings.BuildPlatformTarget)
                .UseToolVersion(ToolSettings.BuildMSBuildToolVersion)
                .WithProperty("TreatWarningsAsErrors", BuildParameters.TreatWarningsAsErrors.ToString())
                .WithTarget("Build")
                .SetMaxCpuCount(ToolSettings.MaxCpuCount)
                .SetConfiguration(BuildParameters.Configuration)
                .WithLogger(
                    Context.Tools.Resolve("MSBuild.ExtensionPack.Loggers.dll").FullPath,
                    "XmlFileLogger",
                    string.Format(
                        "logfile=\"{0}\";invalidCharReplacement=_;verbosity=Detailed;encoding=UTF-8",
                        BuildParameters.Paths.Files.BuildLogFilePath)
                );

            MSBuild(BuildParameters.SolutionFilePath, msbuildSettings);

            // Parse warnings.
            var issues = ReadIssues(
                MsBuildIssuesFromFilePath(
                    BuildParameters.Paths.Files.BuildLogFilePath,
                    MsBuildXmlFileLoggerFormat),
                data.RepositoryRoot);

            Information("{0} MsBuild warnings are found.", issues.Count());
            data.AddIssues(issues);
        }
        else
        {
            var xbuildSettings = new XBuildSettings()
                .SetConfiguration(BuildParameters.Configuration)
                .WithTarget("Build")
                .WithProperty("TreatWarningsAsErrors", "true");

            XBuild(BuildParameters.SolutionFilePath, xbuildSettings);
        }

        CopyBuildOutput();
    }));

BuildParameters.Tasks.DotNetCoreBuildTask = Task("DotNetCoreBuild")
    .IsDependentOn("Clean")
    .IsDependentOn("DotNetCoreRestore")
    .Does(() => {
        Information("Building {0}", BuildParameters.SolutionFilePath);

        var msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", BuildParameters.Version.SemVersion)
                            .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                            .WithProperty("FileVersion",  BuildParameters.Version.Version)
                            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

        if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
        {
            var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

            // Use FrameworkPathOverride when not running on Windows.
            Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
            msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
        }

        DotNetCoreBuild(BuildParameters.SolutionFilePath.FullPath, new DotNetCoreBuildSettings
        {
            Configuration = BuildParameters.Configuration,
            MSBuildSettings = msBuildSettings,
            NoRestore = true
        });

        CopyBuildOutput();
    });

public void CopyBuildOutput()
{
    Information("Copying build output...");

    foreach (var project in ParseSolution(BuildParameters.SolutionFilePath).GetProjects())
    {
        // There is quite a bit of duplication in this function, that really needs to be tidied up at some point
        Information("Project Full Path: {0}", project.Path.FullPath);
        Information("Input BuildPlatformTarget: {0}", ToolSettings.BuildPlatformTarget.ToString());
        var platformTarget = ToolSettings.BuildPlatformTarget == PlatformTarget.MSIL ? "AnyCPU" : ToolSettings.BuildPlatformTarget.ToString();
        Information("Using BuildPlatformTarget: {0}", platformTarget);
        var parsedProject = ParseProject(project.Path, BuildParameters.Configuration, platformTarget);

        if (project.Path.FullPath.ToLower().Contains("wixproj"))
        {
            Warning("Skipping wix project");
            continue;
        }

        if (project.Path.FullPath.ToLower().Contains("shproj"))
        {
            Warning("Skipping shared project");
            continue;
        }

        if (parsedProject.OutputPath == null || parsedProject.RootNameSpace == null || parsedProject.OutputType == null)
        {
            Information("OutputPath: {0}", parsedProject.OutputPath);
            Information("RootNameSpace: {0}", parsedProject.RootNameSpace);
            Information("OutputType: {0}", parsedProject.OutputType);
            throw new Exception(string.Format("Unable to parse project file correctly: {0}", project.Path));
        }

        // Output useful information about the current parsed project. This can be invaluable when digging into issues
        // with the build
        Information("RootNameSpace: {0}", parsedProject.RootNameSpace);
        Information("AssemblyName: {0}", parsedProject.AssemblyName);
        Information("IsLibrary: {0}", parsedProject.IsLibrary());

        if (BuildParameters.IsDotNetCoreBuild)
        {
            Information("IsGlobalTool: {0}", parsedProject.IsGlobalTool());
        }

        Information("IsDotNetCliTestProject: {0}", parsedProject.IsDotNetCliTestProject());
        Information("IsNetCore: {0}", parsedProject.IsNetCore);
        Information("IsNetStandard: {0}", parsedProject.IsNetStandard);
        Information("IsNetFramework: {0}", parsedProject.IsNetFramework);
        Information("IsVS2017ProjectFormat: {0}", parsedProject.IsVS2017ProjectFormat);
        Information("IsFrameworkTestProject: {0}", parsedProject.IsFrameworkTestProject());
        Information("IsTestProject: {0}", parsedProject.IsTestProject());
        Information("IXUnitTestProject: {0}", parsedProject.IsXUnitTestProject());
        Information("IsNUnitTestProject: {0}", parsedProject.IsNUnitTestProject());
        Information("IsWebApplication: {0}", parsedProject.IsWebApplication());

        if (parsedProject.IsWebApplication() && parsedProject.IsNetFramework)
        {
            Information("Project is a Web Application: {0}", parsedProject.AssemblyName);
            var outputFolder = MakeAbsolute(BuildParameters.Paths.Directories.PublishedWebsites.Combine(parsedProject.AssemblyName));

            EnsureDirectoryExists(outputFolder);

            var msbuildSettings = new MSBuildSettings()
                .SetPlatformTarget(ToolSettings.BuildPlatformTarget)
                .UseToolVersion(ToolSettings.BuildMSBuildToolVersion)
                .WithProperty("TreatWarningsAsErrors", BuildParameters.TreatWarningsAsErrors.ToString())
                .WithProperty("OutputPath", outputFolder.FullPath)
                .WithTarget("Build")
                .SetMaxCpuCount(ToolSettings.MaxCpuCount)
                .SetConfiguration(BuildParameters.Configuration)
                .WithLogger(
                    Context.Tools.Resolve("MSBuild.ExtensionPack.Loggers.dll").FullPath,
                    "XmlFileLogger",
                    string.Format(
                        "logfile=\"{0}\";invalidCharReplacement=_;verbosity=Detailed;encoding=UTF-8",
                        BuildParameters.Paths.Files.BuildLogFilePath)
                );

            MSBuild(BuildParameters.SolutionFilePath, msbuildSettings);

            continue;
        }

        // If the project is an exe, then simply copy all of the contents to the correct output folder
        if (!parsedProject.IsLibrary())
        {
            Information("Project has an output type of exe: {0}", parsedProject.AssemblyName);
            var outputFolder = BuildParameters.Paths.Directories.PublishedApplications.Combine(parsedProject.AssemblyName);

            if (parsedProject.AssemblyName == "ChocolateySoftware.ChocolateyManagement.Web.Mvc")
            {
                outputFolder = BuildParameters.Paths.Directories.PublishedApplications.Combine("Web.Mvc");
            }

            EnsureDirectoryExists(outputFolder);

            // If .NET SDK project, copy using dotnet publish for each target framework
            // Otherwise just copy
            if (parsedProject.IsVS2017ProjectFormat)
            {
                var msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", BuildParameters.Version.SemVersion)
                            .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                            .WithProperty("FileVersion",  BuildParameters.Version.Version)
                            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

                if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
                {
                    var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

                    // Use FrameworkPathOverride when not running on Windows.
                    Information("Publish will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
                    msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
                }

                foreach (var targetFramework in parsedProject.NetCore.TargetFrameworks)
                {
                    Information("Running DotNetCorePublish for {0}...", project.Path.FullPath);

                    DotNetCorePublish(project.Path.FullPath, new DotNetCorePublishSettings {
                        OutputDirectory = outputFolder.Combine(targetFramework),

                        Framework = targetFramework,
                        Configuration = BuildParameters.Configuration,
                        MSBuildSettings = msBuildSettings,
                        NoRestore = true,
                        NoBuild = true
                    });
                }
            }
            else
            {
                CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            }

            continue;
        }

        if (parsedProject.IsLibrary() && parsedProject.IsXUnitTestProject())
        {
            Information("Project has an output type of library and is an xUnit Test Project: {0}", parsedProject.AssemblyName);
            var outputFolder = BuildParameters.Paths.Directories.PublishedxUnitTests.Combine(parsedProject.AssemblyName);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else if (parsedProject.IsLibrary() && parsedProject.IsNUnitTestProject())
        {
            Information("Project has an output type of library and is a NUnit Test Project: {0}", parsedProject.AssemblyName);
            var outputFolder = BuildParameters.Paths.Directories.PublishedNUnitTests.Combine(parsedProject.AssemblyName);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else
        {
            Information("Project has an output type of library: {0}", parsedProject.AssemblyName);

            var outputFolder = BuildParameters.Paths.Directories.PublishedLibraries.Combine(parsedProject.AssemblyName);

            if (parsedProject.IsVS2017ProjectFormat)
            {
                var msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", BuildParameters.Version.SemVersion)
                            .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                            .WithProperty("FileVersion",  BuildParameters.Version.Version)
                            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

                if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
                {
                    var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

                    // Use FrameworkPathOverride when not running on Windows.
                    Information("Publish will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
                    msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
                }

                foreach (var targetFramework in parsedProject.NetCore.TargetFrameworks)
                {
                    Information("Running DotNetCorePublish for {0}...", project.Path.FullPath);

                    DotNetCorePublish(project.Path.FullPath, new DotNetCorePublishSettings {
                        OutputDirectory = outputFolder.Combine(targetFramework),

                        Framework = targetFramework,
                        Configuration = BuildParameters.Configuration,
                        MSBuildSettings = msBuildSettings,
                        NoRestore = true,
                        NoBuild = true
                    });
                }
            }
            else
            {
                EnsureDirectoryExists(outputFolder);
                Information(parsedProject.OutputPath.FullPath);
                CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            }

            continue;
        }
    }
}

BuildParameters.Tasks.PackageTask = Task("Package");
BuildParameters.Tasks.DefaultTask = Task("Default")
    .IsDependentOn("Package");


BuildParameters.Tasks.UploadArtifactsTask = Task("Upload-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => DirectoryExists(BuildParameters.Paths.Directories.NuGetPackages) || DirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages), "Skipping because no packages to upload")
    .Does(() =>
{
    // Concatenating FilePathCollections should make sure we get unique FilePaths
    foreach (var package in GetFiles(BuildParameters.Paths.Directories.Packages + "/*") +
                           GetFiles(BuildParameters.Paths.Directories.NuGetPackages + "/*") +
                           GetFiles(BuildParameters.Paths.Directories.ChocolateyPackages + "/*"))
    {
        BuildParameters.BuildProvider.UploadArtifact(package);
    }
});

BuildParameters.Tasks.ContinuousIntegrationTask = Task("CI")
    .IsDependentOn("Upload-Artifacts")
    .IsDependentOn("Publish-PreRelease-Packages")
    .IsDependentOn("Publish-Release-Packages")
    .IsDependentOn("Publish-GitHub-Release")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of " + BuildParameters.Title + ".  All publishing tasks have been attempted.");
    }
});

BuildParameters.Tasks.ReleaseNotesTask = Task("ReleaseNotes")
  .IsDependentOn("Create-Release-Notes");

BuildParameters.Tasks.LabelsTask = Task("Labels")
  .IsDependentOn("Create-Default-Labels");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

public Builder Build
{
    get
    {
        return new Builder(target => RunTarget(target));
    }
}

public class Builder
{
    private Action<string> _action;

    public Builder(Action<string> action)
    {
        _action = action;
    }

    public void Run()
    {
        BuildParameters.IsDotNetCoreBuild = false;

        SetupTasks(BuildParameters.IsDotNetCoreBuild);

        _action(BuildParameters.Target);
    }

    public void RunDotNetCore()
    {
        BuildParameters.IsDotNetCoreBuild = true;

        SetupTasks(BuildParameters.IsDotNetCoreBuild);

        _action(BuildParameters.Target);
    }

    private static void SetupTasks(bool isDotNetCoreBuild)
    {
        var prefix = isDotNetCoreBuild ? "DotNetCore" : "";

        BuildParameters.Tasks.PackageTask.IsDependentOn("Test");
        BuildParameters.Tasks.PackageTask.IsDependentOn("Analyze");
        BuildParameters.Tasks.PackageTask.IsDependentOn("Create-Chocolatey-Packages");
        BuildParameters.Tasks.PackageTask.IsDependentOn("Create-NuGet-Packages");
        BuildParameters.Tasks.CreateChocolateyPackagesTask.IsDependentOn("Configuration-Builder");
        BuildParameters.Tasks.CreateChocolateyPackagesTask.IsDependentOn("Obfuscate-Assemblies");
        BuildParameters.Tasks.CreateChocolateyPackagesTask.IsDependentOn("Sign-Assemblies");
        BuildParameters.Tasks.CreateNuGetPackagesTask.IsDependentOn("Sign-PowerShellScripts");
        BuildParameters.Tasks.CreateNuGetPackagesTask.IsDependentOn("Sign-Assemblies");
        BuildParameters.Tasks.CreateChocolateyPackagesTask.IsDependentOn("Sign-PowerShellScripts");
        BuildParameters.Tasks.CreateChocolateyPackagesTask.IsDependentOn(prefix + "Build");
        BuildParameters.Tasks.ObfuscateAssembliesTask.IsDependeeOf("Sign-Assemblies");
        BuildParameters.Tasks.StrongNameSignerTask.IsDependentOn(prefix + "Restore");
        BuildParameters.Tasks.StrongNameSignerTask.IsDependeeOf(prefix + "Build");
        BuildParameters.Tasks.ChangeStrongNameSignatures.IsDependentOn(prefix + "Restore");
        BuildParameters.Tasks.ChangeStrongNameSignatures.IsDependeeOf(prefix + "Build");
        BuildParameters.Tasks.ObfuscateAssembliesTask.IsDependentOn(prefix + "Build");
        BuildParameters.Tasks.InspectCodeTask.IsDependentOn(prefix + "Build");
        BuildParameters.Tasks.ConfigurationBuilderTask.IsDependentOn(prefix + "Build");
        BuildParameters.Tasks.TestTask.IsDependentOn(prefix + "Build");

        if (!isDotNetCoreBuild)
        {
            if (BuildParameters.TransifexEnabled)
            {
                BuildParameters.Tasks.BuildTask.IsDependentOn("Transifex-Pull-Translations");
            }

            BuildParameters.Tasks.TestNUnitTask.IsDependentOn(prefix + "Build");
            BuildParameters.Tasks.TestxUnitTask.IsDependentOn(prefix + "Build");
            BuildParameters.Tasks.TestTask.IsDependentOn("Test-NUnit");
            BuildParameters.Tasks.TestTask.IsDependentOn("Test-xUnit");
            BuildParameters.Tasks.GenerateLocalCoverageReportTask.IsDependentOn("Test-NUnit");
            BuildParameters.Tasks.GenerateLocalCoverageReportTask.IsDependentOn("Test-xUnit");
            BuildParameters.Tasks.TestTask.IsDependentOn("Generate-FriendlyTestReport");
            BuildParameters.Tasks.TestTask.IsDependentOn("Generate-LocalCoverageReport");
            BuildParameters.Tasks.TestTask.IsDependentOn("Report-Code-Coverage-Metrics");
        }
        else
        {
            if (BuildParameters.TransifexEnabled)
            {
                BuildParameters.Tasks.DotNetCoreBuildTask.IsDependentOn("Transifex-Pull-Translations");
            }

            BuildParameters.Tasks.PackageTask.IsDependentOn(prefix + "Pack");
            BuildParameters.Tasks.GenerateLocalCoverageReportTask.IsDependentOn(prefix + "Test");
            BuildParameters.Tasks.TestTask.IsDependentOn("Generate-LocalCoverageReport");
            BuildParameters.Tasks.TestTask.IsDependentOn("Report-Code-Coverage-Metrics");
        }
    }
}