public enum BranchType
{
    Unknown,
    HotFix,
    Release,
    Develop,
    Master
}

public static class BuildParameters
{
    public static string CertificateFilePath { get; private set; }
    public static string CertificatePassword { get; private set; }
    public static string CertificateTimestampUrl { get; private set; }
    public static string CertificateAlgorithm { get; private set; }
    public static string CertificateSubjectName { get; private set; }
    public static string StrongNameKeyPath { get; private set; }
    public static string PreReleaseLabelFilePath { get; private set; }
    public static string Target { get; private set; }
    public static string BuildCounter { get; private set; }
    public static string Configuration { get; private set; }
    public static string DeploymentEnvironment { get; private set;}
    public static Cake.Core.Configuration.ICakeConfiguration CakeConfiguration { get; private set; }
    public static bool IsLocalBuild { get; private set; }
    public static bool IsRunningOnGitHubActions { get; private set; }
    public static bool IsRunningOnTeamCity { get; private set; }
    public static bool IsRepositoryHostedOnGitHub { get; private set; }
    public static PlatformFamily BuildAgentOperatingSystem { get; private set; }
    public static bool IsPullRequest { get; private set; }
    public static bool IsMainRepository { get; private set; }
    public static string MasterBranchName { get; private set; }
    public static string DevelopBranchName { get; private set; }
    public static bool PrepareLocalRelease { get; set; }
    public static BranchType BranchType { get; private set; }
    public static bool IsTagged { get; private set; }
    public static bool IsDotNetCoreBuild { get; set; }
    public static bool TreatWarningsAsErrors { get; set; }
    public static BuildVersion Version { get; private set; }
    public static BuildPaths Paths { get; private set; }
    public static BuildTasks Tasks { get; set; }
    public static DirectoryPath RootDirectoryPath { get; private set; }
    public static FilePath SolutionFilePath { get; private set; }
    public static DirectoryPath SourceDirectoryPath { get; private set; }
    public static DirectoryPath SolutionDirectoryPath { get; private set; }
    public static DirectoryPath TestDirectoryPath { get; private set; }
    public static FilePath IntegrationTestScriptPath { get; private set; }
    public static string TestFilePattern { get; private set; }
    public static string Title { get; private set; }
    public static string ResharperSettingsFileName { get; private set; }
    public static string RepositoryOwner { get; private set; }
    public static string RepositoryName { get; private set; }
    public static string ProductName { get; private set; }
    public static string ProductDescription { get; private set; }
    public static string ProductCopyright { get; private set; }
    public static bool ProductComVisible { get; private set; }
    public static bool ProductClsCompliant { get; private set; }
    public static string ProductCompany { get; private set; }
    public static string ProductTrademark { get; private set; }
    public static ICollection<AssemblyInfoCustomAttribute> ProductCustomAttributes { get; private set; }
    public static bool ObfuscateAssembly { get; private set; }
    public static bool ShouldRunInspectCode { get; private set; }
    public static bool ShouldRunDotNetCorePack { get; private set; }
    public static bool ShouldBuildNugetSourcePackage { get; private set; }

    public static bool ShouldStrongNameOutputAssemblies { get; private set; }
    public static bool ShouldObfuscateOutputAssemblies { get; private set; }
    public static bool ShouldAuthenticodeSignOutputAssemblies { get; private set; }

    public static bool ShouldAuthenticodeSignPowerShellScripts { get; private set; }
    public static bool ShouldStrongNameSignDependentAssemblies { get; private set; }
    public static string StrongNameDependentAssembliesInputPath { get; private set; }
    public static bool ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken { get; private set; }
    public static string AssemblyNamesRegexPattern { get; private set; }
    public static bool UseChocolateyGuiStrongNameKey { get; private set; }

    public static FilePath NugetConfig { get; private set; }
    public static ICollection<string> NuGetSources { get; private set; }
    public static DirectoryPath RestorePackagesDirectory { get; private set; }
    public static Func<FilePathCollection> GetFilesToObfuscate { get; private set; }
    public static Func<FilePathCollection> GetFilesToSign { get; private set; }
    public static Func<FilePathCollection> GetScriptsToSign { get; private set; }
    public static Func<FilePathCollection> GetProjectsToPack { get; private set; }
    public static List<PackageSourceData> PackageSources { get; private set; }
    public static bool ForceContinuousIntegration { get; private set; }
    public static List<string> AllowedAssemblyNames { get; private set; }
    public static IBuildProvider BuildProvider { get; private set; }

    public static bool ShouldPublishGitHub { get; private set; }
    public static bool ShouldDownloadMilestoneReleaseNotes { get; private set;}
    public static bool ShouldDownloadFullReleaseNotes { get; private set;}

    public static FilePath MilestoneReleaseNotesFilePath { get; private set; }
    public static FilePath FullReleaseNotesFilePath { get; private set; }

    public static GitHubCredentials GitHub { get; private set; }

    static BuildParameters()
    {
        Tasks = new BuildTasks();
    }

    public static bool CanUseGitReleaseManager
    {
        get
        {
            return !string.IsNullOrEmpty(BuildParameters.GitHub.Token);
        }
    }

    public static void SetBuildVersion(BuildVersion version)
    {
        Version  = version;
    }

    public static void SetBuildPaths(BuildPaths paths)
    {
        Paths = paths;
    }

    public static void PrintParameters(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        context.Information("Printing Build Parameters...");
        context.Information("------------------------------------------------------------------------------------------");
        context.Information("Target: {0}", Target);
        context.Information("Configuration: {0}", Configuration);
        context.Information("IsLocalBuild: {0}", IsLocalBuild);
        context.Information("IsPullRequest: {0}", IsPullRequest);
        context.Information("IsTagged: {0}", IsTagged);
        context.Information("IsMainRepository: {0}", IsMainRepository);
        context.Information("PrepareLocalRelease: {0}", BuildParameters.PrepareLocalRelease);
        context.Information("ShouldDownloadMilestoneReleaseNotes: {0}", BuildParameters.ShouldDownloadMilestoneReleaseNotes);
        context.Information("ShouldDownloadFullReleaseNotes: {0}", BuildParameters.ShouldDownloadFullReleaseNotes);
        context.Information("Repository Name: {0}", BuildProvider.Repository.Name);
        context.Information("Branch Type: {0}", BranchType);
        context.Information("Branch Name: {0}", BuildProvider.Repository.Branch);
        context.Information("IsDotNetCoreBuild: {0}", IsDotNetCoreBuild);
        context.Information("Solution FilePath: {0}", context.MakeAbsolute((FilePath)SolutionFilePath));
        context.Information("Solution DirectoryPath: {0}", context.MakeAbsolute((DirectoryPath)SolutionDirectoryPath));
        context.Information("Source DirectoryPath: {0}", context.MakeAbsolute(SourceDirectoryPath));
        context.Information("Build DirectoryPath: {0}", context.MakeAbsolute(Paths.Directories.Build));
        context.Information("TreatWarningsAsErrors: {0}", TreatWarningsAsErrors);
        context.Information("BuildAgentOperatingSystem: {0}", BuildAgentOperatingSystem);
        context.Information("RepositoryOwner: {0}", RepositoryOwner);
        context.Information("RepositoryName: {0}", RepositoryName);
        context.Information("NugetConfig: {0} ({1})", NugetConfig, context.FileExists(NugetConfig));
        context.Information("Build Counter: {0}", BuildCounter);
        context.Information("RestorePackagesDirectory: {0}", RestorePackagesDirectory);
        context.Information("ProductName: {0}", ProductName);
        context.Information("ProductDescription: {0}", ProductDescription);
        context.Information("ProductCopyright: {0}", ProductCopyright);
        context.Information("ProductComVisible: {0}", ProductComVisible);
        context.Information("ProductClsCompliant: {0}", ProductClsCompliant);
        context.Information("ProductCompany: {0}", ProductCompany);
        context.Information("ProductTrademark: {0}", ProductTrademark);
        context.Information("ObfuscateAssembly: {0}", ObfuscateAssembly);
        context.Information("ForceContinuousIntegration: {0}", ForceContinuousIntegration);
        context.Information("NuGetSources: {0}", string.Join(", ", NuGetSources));
        context.Information("StrongNameDependentAssembliesInputPath: {0}", StrongNameDependentAssembliesInputPath);
        context.Information("ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken: {0}", ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken);
        context.Information("AssemblyNamesRegexPattern: {0}", AssemblyNamesRegexPattern);
        context.Information("UseChocolateyGuiStrongNameKey: {0}", UseChocolateyGuiStrongNameKey);
        context.Information("AllowedAssemblyName: {0}", string.Join(", ", AllowedAssemblyNames));

        if (ProductCustomAttributes != null)
        {
            context.Information("ProductCustomAttributes: {0}", string.Join(", ", ProductCustomAttributes));
        }
        else
        {
            context.Information("No Product Custom Attributes being used");
        }

        context.Information("------------------------------------------------------------------------------------------");
    }

    public static void SetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        DirectoryPath sourceDirectoryPath,
        string title,
        FilePath solutionFilePath = null,
        DirectoryPath solutionDirectoryPath = null,
        DirectoryPath rootDirectoryPath = null,
        DirectoryPath testDirectoryPath = null,
        string testFilePattern = null,
        string integrationTestScriptPath = null,
        string resharperSettingsFileName = null,
        string repositoryOwner = null,
        string repositoryName = null,
        bool shouldRunInspectCode = true,
        bool shouldRunDotNetCorePack = false,
        bool shouldBuildNugetSourcePackage = false,
        bool shouldStrongNameOutputAssemblies = true,
        bool shouldObfuscateOutputAssemblies = true,
        bool shouldAuthenticodeSignOutputAssemblies = true,
        bool shouldAuthenticodeSignPowerShellScripts = true,
        bool shouldStrongNameSignDependentAssemblies = true,
        bool useChocolateyGuiStrongNameKey = false,
        string strongNameDependentAssembliesInputPath = null,
        bool shouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken = false,
        string assemblyNamesRegexPattern = null,
        FilePath nugetConfig = null,
        ICollection<string> nuGetSources = null,
        bool treatWarningsAsErrors = true,
        DirectoryPath restorePackagesDirectory = null,
        Func<FilePathCollection> getFilesToObfuscate = null,
        Func<FilePathCollection> getFilesToSign = null,
        Func<FilePathCollection> getScriptsToSign = null,
        Func<FilePathCollection> getProjectsToPack = null,
        string productName = null,
        string productDescription = null,
        string productCopyright = null,
        bool productComVisible = false,
        bool productClsCompliant = false,
        string productCompany = null,
        string productTrademark = null,
        bool obfuscateAssembly = false,
        ICollection<AssemblyInfoCustomAttribute> productCustomAttributes = null,
        List<PackageSourceData> packageSourceDatas = null,
        List<string> allowedAssemblyNames = null,
        string certificateSubjectName = null,
        bool shouldPublishGitHub = false,
        string masterBranchName = "master",
        string developBranchName = "develop",
        bool shouldDownloadMilestoneReleaseNotes = false,
        bool shouldDownloadFullReleaseNotes = false,
        FilePath milestoneReleaseNotesFilePath = null,
        FilePath fullReleaseNotesFilePath = null
        )
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        CertificateSubjectName = certificateSubjectName ?? "Chocolatey Software, Inc.";
        CertificateFilePath = context.EnvironmentVariable("CHOCOLATEY_OFFICIAL_CERT") ?? "";
        CertificatePassword = context.EnvironmentVariable("CHOCOLATEY_OFFICIAL_CERT_PASSWORD") ?? "";
        CertificateTimestampUrl = context.EnvironmentVariable("CERT_TIMESTAMP_URL") ?? "http://timestamp.digicert.com";
        CertificateAlgorithm = context.EnvironmentVariable("CERT_ALGORITHM") ?? "Sha256";
        PreReleaseLabelFilePath = ".build_pre_release_label";

        BuildProvider = GetBuildProvider(context, buildSystem);

        IsTagged = BuildProvider.Repository.Tag.IsTag;
        IsRunningOnGitHubActions = BuildProvider.Type == BuildProviderType.GitHubActions;
        IsRunningOnTeamCity = BuildProvider.Type == BuildProviderType.TeamCity;

        MasterBranchName = masterBranchName;
        DevelopBranchName = developBranchName;

        SourceDirectoryPath = sourceDirectoryPath;
        Title = title;
        SolutionFilePath = solutionFilePath ?? SourceDirectoryPath.CombineWithFilePath(Title + ".sln");
        SolutionDirectoryPath = solutionDirectoryPath ?? SourceDirectoryPath.Combine(Title);
        RootDirectoryPath = rootDirectoryPath ?? context.MakeAbsolute(context.Environment.WorkingDirectory);
        TestDirectoryPath = testDirectoryPath ?? sourceDirectoryPath;
        TestFilePattern = testFilePattern;
        IntegrationTestScriptPath = integrationTestScriptPath ?? context.MakeAbsolute((FilePath)"test.cake");
        ResharperSettingsFileName = resharperSettingsFileName ?? string.Format("{0}.sln.DotSettings", Title);
        RepositoryOwner = repositoryOwner ?? string.Empty;
        RepositoryName = repositoryName ?? Title;

        ShouldRunInspectCode = shouldRunInspectCode;
        ShouldRunDotNetCorePack = shouldRunDotNetCorePack;
        ShouldBuildNugetSourcePackage = shouldBuildNugetSourcePackage;

        ShouldStrongNameOutputAssemblies = shouldStrongNameOutputAssemblies;
        ShouldObfuscateOutputAssemblies = shouldObfuscateOutputAssemblies;
        ShouldAuthenticodeSignOutputAssemblies = shouldAuthenticodeSignOutputAssemblies;
        ShouldAuthenticodeSignPowerShellScripts = shouldAuthenticodeSignPowerShellScripts;
        ShouldStrongNameSignDependentAssemblies = shouldStrongNameSignDependentAssemblies;
        StrongNameDependentAssembliesInputPath = strongNameDependentAssembliesInputPath ?? SourceDirectoryPath.Combine("packages").FullPath;
        ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken = shouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken;
        AssemblyNamesRegexPattern = assemblyNamesRegexPattern ?? "chocolatey.lib|chocolatey-licensed.lib|ChocolateyGui.Common|ChocolateyGui.Common.Windows";
        UseChocolateyGuiStrongNameKey = useChocolateyGuiStrongNameKey;

        NugetConfig = context.MakeAbsolute(nugetConfig ?? (FilePath)"./NuGet.Config");
        NuGetSources = nuGetSources;
        RestorePackagesDirectory = restorePackagesDirectory;
        GetFilesToObfuscate = getFilesToObfuscate;
        GetFilesToSign = getFilesToSign;
        GetScriptsToSign = getScriptsToSign;
        GetProjectsToPack = getProjectsToPack;
        ProductName = productName ?? "Name not provided";
        ProductDescription = productDescription ?? "Description not provided";
        ProductCopyright = productCopyright ?? "Copyright not provided";
        ProductComVisible = productComVisible;
        ProductClsCompliant = productClsCompliant;
        ProductCompany = productCompany ?? "Chocolatey Software, Inc.";
        ProductTrademark = productTrademark ?? "Chocolatey - Chocolatey Software, Inc.";
        ObfuscateAssembly = obfuscateAssembly;
        ProductCustomAttributes = productCustomAttributes;

        if (nuGetSources == null)
        {
            if (context.FileExists(NugetConfig))
            {
                NuGetSources = (
                                    from configuration in System.Xml.Linq.XDocument.Load(NugetConfig.FullPath).Elements("configuration")
                                    from packageSources in configuration.Elements("packageSources")
                                    from add in packageSources.Elements("add")
                                    from value in add.Attributes("value")
                                    select value.Value
                                ).ToArray();

                context.Information("NuGet Sources configured from nuget.config file.");
            }
            else
            {
                var primaryNuGetSource = context.EnvironmentVariable("PRIMARY_NUGET_SOURCE");

                if (!string.IsNullOrEmpty(primaryNuGetSource))
                {
                    NuGetSources = new []{
                        primaryNuGetSource
                    };

                    context.Information("NuGet Sources configured using primary NuGet Source.");
                }
                else
                {
                    NuGetSources = new []{
                        "https://www.nuget.org/api/v2/",
                        "https://api.nuget.org/v3/index.json"
                    };

                    context.Information("NuGet Sources configured using default values.");
                }
            }
        }

        Target = context.Argument("target", "Default");
        BuildCounter = context.Argument("buildCounter", BuildProvider.Build.Number);
        Configuration = context.Argument("configuration", "Release");
        DeploymentEnvironment = context.Argument("environment", "Release");
        ForceContinuousIntegration = context.Argument("forceContinuousIntegration", false);
        PrepareLocalRelease = context.Argument("prepareLocalRelease", false);
        CakeConfiguration = context.GetConfiguration();
        IsLocalBuild = buildSystem.IsLocalBuild;

        MilestoneReleaseNotesFilePath = milestoneReleaseNotesFilePath ?? RootDirectoryPath.CombineWithFilePath("CHANGELOG.md");
        FullReleaseNotesFilePath = fullReleaseNotesFilePath ?? RootDirectoryPath.CombineWithFilePath("ReleaseNotes.md");

        if (ShouldStrongNameOutputAssemblies || ShouldStrongNameSignDependentAssemblies || ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken)
        {
            var officialStrongNameKey = context.EnvironmentVariable("CHOCOLATEY_OFFICIAL_KEY");
            var localUnofficialStrongNameKey = RootDirectoryPath.CombineWithFilePath("chocolatey.snk").FullPath;

            if (UseChocolateyGuiStrongNameKey)
            {
                context.Information("Switching to Chocolatey GUI Strong Name Key selection...");
                officialStrongNameKey = context.EnvironmentVariable("CHOCOLATEYGUI_OFFICIAL_KEY");
                localUnofficialStrongNameKey = RootDirectoryPath.CombineWithFilePath("chocolateygui.snk").FullPath;
            }

            if (Configuration == "ReleaseOfficial" && !string.IsNullOrWhiteSpace(officialStrongNameKey) && context.FileExists(officialStrongNameKey))
            {
                context.Information("Using Official Strong Name Key...");
                StrongNameKeyPath = officialStrongNameKey;
            }
            else if (context.FileExists(localUnofficialStrongNameKey))
            {
                context.Information("Using local Unofficial Strong Name Key...");
                StrongNameKeyPath = localUnofficialStrongNameKey;
            }
            else
            {
                context.Information("Creating new unofficial Strong Name Key...");

                var newChocolateyUnofficialKey = context.MakeAbsolute(new FilePath(string.Format("{0}.unofficial.snk", Title)));

                // If the file already exists, don't re-create it
                if (!context.FileExists(newChocolateyUnofficialKey))
                {
                    context.StrongNameCreate(newChocolateyUnofficialKey);
                }

                StrongNameKeyPath = newChocolateyUnofficialKey.FullPath;
            }
        }

        AllowedAssemblyNames = allowedAssemblyNames ?? new List<string> { "chocolatey.dll", "chocolatey.licensed.dll", "ChocolateyGui.Common.dll", "ChocolateyGui.Common.Windows.dll" };

        BuildAgentOperatingSystem = context.Environment.Platform.Family;

        IsPullRequest = BuildProvider.PullRequest.IsPullRequest;
        IsMainRepository = StringComparer.OrdinalIgnoreCase.Equals(string.Concat(repositoryOwner, "/", RepositoryName), BuildProvider.Repository.Name);

        var branchName = BuildProvider.Repository.Branch;
        if (StringComparer.OrdinalIgnoreCase.Equals(masterBranchName, branchName))
        {
            BranchType = BranchType.Master;
        }
        else if (StringComparer.OrdinalIgnoreCase.Equals(developBranchName, branchName))
        {
            BranchType = BranchType.Develop;
        }
        else if (branchName.StartsWith("release", StringComparison.OrdinalIgnoreCase))
        {
            BranchType = BranchType.Release;
        }
        else if (branchName.StartsWith("hotfix", StringComparison.OrdinalIgnoreCase))
        {
            BranchType = BranchType.HotFix;
        }
        else
        {
            BranchType = BranchType.Unknown;
        }

        TreatWarningsAsErrors = treatWarningsAsErrors;

        GitHub = GetGitHubCredentials(context);

        SetBuildPaths(BuildPaths.GetPaths());

        ShouldDownloadFullReleaseNotes = shouldDownloadFullReleaseNotes;
        ShouldDownloadMilestoneReleaseNotes = shouldDownloadMilestoneReleaseNotes;

        ShouldPublishGitHub = (!IsLocalBuild &&
                                !IsPullRequest &&
                                IsMainRepository &&
                                (BuildParameters.BranchType == BranchType.Master || BuildParameters.BranchType == BranchType.Release || BuildParameters.BranchType == BranchType.HotFix) &&
                                IsTagged &&
                                shouldPublishGitHub);

        if (packageSourceDatas?.Any() ?? false)
        {
            context.Information("Setting Package Sources to passed in variable...");
            PackageSources = packageSourceDatas;
        }
        else
        {
            PackageSources = new List<PackageSourceData>();

            var defaultPushSourceUrl = context.EnvironmentVariable(Environment.DefaultPushSourceUrlVariable);
            context.Information("defaultPushSourceUrl: {0}", defaultPushSourceUrl);
            if (!string.IsNullOrEmpty(defaultPushSourceUrl))
            {
                context.Information("Adding Default Package Source Datas...");
                var defaultPushSourceUrlParts = Environment.DefaultPushSourceUrlVariable.Split('_');
                PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl));
                PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.Chocolatey));
                PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.NuGet, false));
                PackageSources.Add(new PackageSourceData(context, defaultPushSourceUrlParts[0], defaultPushSourceUrl, FeedType.Chocolatey, false));
            }
        }
    }
}
