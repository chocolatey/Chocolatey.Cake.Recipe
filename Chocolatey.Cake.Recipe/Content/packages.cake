// Copyright © 2022 Chocolatey Software, Inc
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

BuildParameters.Tasks.CopyNuspecFolderTask = Task("Copy-Nuspec-Folders")
    .Does(() =>
{
    if (DirectoryExists("./nuspec/chocolatey"))
    {
        EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory);
        CopyFiles(GetFiles("./nuspec/chocolatey/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory, true);
    }

    if (DirectoryExists("./nuspec/nuget"))
    {
        EnsureDirectoryExists(BuildParameters.Paths.Directories.NuGetNuspecDirectory);
        CopyFiles(GetFiles("./nuspec/nuget/**/*"), BuildParameters.Paths.Directories.NuGetNuspecDirectory, true);
    }
});

BuildParameters.Tasks.CreateChocolateyPackagesTask = Task("Create-Chocolatey-Packages")
    .IsDependentOn("Clean")
    .IsDependentOn("Copy-Nuspec-Folders")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .WithCriteria(() => DirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory), "Skipping because Chocolatey nuspec directory is missing")
    .Does(() =>
{
    var nuspecFiles = GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + BuildParameters.ChocolateyNuspecGlobbingPattern);

    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages);

    foreach (var nuspecFile in nuspecFiles)
    {
        // TODO: Addin the release notes
        // ReleaseNotes = BuildParameters.ReleaseNotes.Notes.ToArray(),

        // Create package.
        ChocolateyPack(nuspecFile, new ChocolateyPackSettings {
            AllowUnofficial = true,
            Version = BuildParameters.Version.PackageVersion,
            OutputDirectory = BuildParameters.Paths.Directories.ChocolateyPackages,
            WorkingDirectory = BuildParameters.Paths.Directories.PublishedApplications,
            Copyright = BuildParameters.ProductCopyright
        });
    }
});

BuildParameters.Tasks.CreateNuGetPackagesTask = Task("Create-NuGet-Packages")
    .IsDependentOn("Clean")
    .IsDependentOn("Copy-Nuspec-Folders")
    .WithCriteria(() => BuildParameters.ShouldRunNuGet, "Skipping because execution of NuGet has been disabled")
    .WithCriteria(() => DirectoryExists(BuildParameters.Paths.Directories.NuGetNuspecDirectory), "Skipping because NuGet nuspec directory does not exist")
    .Does(() =>
{
    var nuspecFiles = GetFiles(BuildParameters.Paths.Directories.NuGetNuspecDirectory + BuildParameters.NuGetNuspecGlobbingPattern);

    EnsureDirectoryExists(BuildParameters.Paths.Directories.NuGetPackages);

    foreach (var nuspecFile in nuspecFiles)
    {
        // TODO: Addin the release notes
        // ReleaseNotes = BuildParameters.ReleaseNotes.Notes.ToArray(),

        if (DirectoryExists(BuildParameters.Paths.Directories.PublishedLibraries.Combine(nuspecFile.GetFilenameWithoutExtension().ToString())))
        {
            // Create packages.
            NuGetPack(nuspecFile, new NuGetPackSettings {
                Version = BuildParameters.Version.PackageVersion,
                BasePath = BuildParameters.Paths.Directories.PublishedLibraries.Combine(nuspecFile.GetFilenameWithoutExtension().ToString()),
                OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
                Symbols = false,
                NoPackageAnalysis = true,
                Copyright = BuildParameters.ProductCopyright
            });

            continue;
        }

        if (DirectoryExists(BuildParameters.Paths.Directories.PublishedApplications.Combine(nuspecFile.GetFilenameWithoutExtension().ToString())))
        {
            // Create packages.
            NuGetPack(nuspecFile, new NuGetPackSettings {
                Version = BuildParameters.Version.PackageVersion,
                BasePath = BuildParameters.Paths.Directories.PublishedApplications.Combine(nuspecFile.GetFilenameWithoutExtension().ToString()),
                OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
                Symbols = false,
                NoPackageAnalysis = true,
                Copyright = BuildParameters.ProductCopyright
            });

            continue;
        }

        // Create packages.
        NuGetPack(nuspecFile, new NuGetPackSettings {
            Version = BuildParameters.Version.PackageVersion,
            OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
            Symbols = false,
            NoPackageAnalysis = true,
            Copyright = BuildParameters.ProductCopyright
        });
    }
});

BuildParameters.Tasks.DotNetPackTask = Task("DotNetPack")
    .IsDependentOn("DotNetBuild")
    .WithCriteria(() => BuildParameters.ShouldRunDotNetPack, "Skipping because packaging through .NET Core is disabled")
    .Does(() =>
{
    var projects = GetFiles(BuildParameters.SourceDirectoryPath + "/**/*.csproj")
        - GetFiles(BuildParameters.RootDirectoryPath + "/tools/**/*.csproj")
        - GetFiles(BuildParameters.SourceDirectoryPath + "/**/*.Tests.csproj")
        - GetFiles(BuildParameters.SourceDirectoryPath + "/packages/**/*.csproj");

    // This allows the consumer of the Chocolatey Cake Recipe to control directly
    // the projects that are packed at the end of the build process.
    if (BuildParameters.GetProjectsToPack != null)
    {
        Information("Replacing list of projects to pack...");
        projects = BuildParameters.GetProjectsToPack();
    }

    var msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", BuildParameters.Version.PackageVersion)
                            .WithProperty("AssemblyVersion", BuildParameters.Version.FileVersion)
                            .WithProperty("FileVersion",  BuildParameters.Version.FileVersion)
                            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion)
                            .WithProperty("Copyright", BuildParameters.ProductCopyright);

    if (BuildParameters.ShouldBuildNuGetSourcePackage)
    {
        msBuildSettings.WithProperty("SymbolPackageFormat", "snupkg");
    }

    var settings = new DotNetCorePackSettings {
        NoBuild = true,
        NoRestore = true,
        Configuration = BuildParameters.Configuration,
        OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
        MSBuildSettings = msBuildSettings,
        IncludeSource = BuildParameters.ShouldBuildNuGetSourcePackage,
        IncludeSymbols = BuildParameters.ShouldBuildNuGetSourcePackage,
    };

    foreach (var project in projects)
    {
        DotNetCorePack(project.ToString(), settings);
    }
});

BuildParameters.Tasks.PublishPreReleasePackagesTask = Task("Publish-PreRelease-Packages")
    .WithCriteria(() => BuildParameters.ShouldPublishPreReleasePackages, "Skipping because publishing of pre-release packages is not enabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => !BuildParameters.IsTagged, "Skipping because current commit is tagged")
    .IsDependentOn("Package")
    .Does(() =>
{
    var chocolateySources = BuildParameters.PackageSources.Where(p => p.Type == FeedType.Chocolatey && p.IsRelease == false).ToList();
    var nugetSources = BuildParameters.PackageSources.Where(p => p.Type == FeedType.NuGet && p.IsRelease == false).ToList();

    PushChocolateyPackages(Context, false, chocolateySources);

    PushNuGetPackages(Context, false, nugetSources);
})
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-PreRelease-Packages Task failed, but continuing with next Task...");
    publishingError = true;
});

BuildParameters.Tasks.PublishReleasePackagesTask = Task("Publish-Release-Packages")
    .WithCriteria(() => BuildParameters.ShouldPublishReleasePackages, "Skipping because publishing of release packages is not enabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because current commit is not tagged")
    .IsDependentOn("Package")
    .Does(() =>
{
    var chocolateySources = BuildParameters.PackageSources.Where(p => p.Type == FeedType.Chocolatey && p.IsRelease == true).ToList();
    var nugetSources = BuildParameters.PackageSources.Where(p => p.Type == FeedType.NuGet && p.IsRelease == true).ToList();

    PushChocolateyPackages(Context, true, chocolateySources);

    PushNuGetPackages(Context, true, nugetSources);
})
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-Release-Packages Task failed, but continuing with next Task...");
    publishingError = true;
});

BuildParameters.Tasks.PublishAwsLambdasTask = Task("Publish-AWS-Lambdas")
    .WithCriteria(() => BuildParameters.ShouldPublishAwsLambdas, "Skipping because publishing of AWS Lambdas is not enabled")
    .WithCriteria(() => BuildParameters.IsDotNetBuild, "Skipping since runing DotNet related tasks hasn't been selected in the recipe.cake file.")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration, "Skipping because this is a local build, and force isn't being applied")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because current commit is not tagged")
    .Does(() => RequireTool(ToolSettings.AmazonLambdaGlobalTool, () => {
        if (DirectoryExists(BuildParameters.Paths.Directories.PublishedLambdas))
        {
            var lambdaPackages = GetFiles(BuildParameters.Paths.Directories.PublishedLambdas + "/**/*.zip");
            var regexPattern = new System.Text.RegularExpressions.Regex(@"(.*).((0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$)");

            foreach (var lambdaPackage in lambdaPackages)
            {
                Information("Deploying Lambda package from zip file: {0}", lambdaPackage);

                var functionName = lambdaPackage.GetFilenameWithoutExtension().FullPath.ToLower();

                // We need to test if the lambda package contains a semantic version, and if it does, remove it.
                // We "remove" it, by using the first matching group of the regular expression
                var matches = regexPattern.Matches(functionName);
                if (matches.Count == 1)
                {
                    functionName = matches[0].Groups[1].Value;
                }

                Information("AWS Function Name: {0}", functionName);

                StartProcess("./tools/dotnet-lambda.exe", new ProcessSettings { Arguments = string.Format("deploy-function --package-type zip --package {0} --disable-interactive true --function-name {1}-test", lambdaPackage, functionName) });
            }
        }
        else
        {
            Information("Unable to publish AWS Lambdas as AWS Lambdas Directory doesn't exist.");
        }
    })
)
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-AWS-Lambdas Task failed, but continuing with next Task...");
    publishingError = true;
});

public void PushChocolateyPackages(ICakeContext context, bool isRelease, List<PackageSourceData> chocolateySources)
{
    if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows && DirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages))
    {
        Information("Number of configured {0} Chocolatey Sources: {1}", isRelease ? "Release" : "PreRelease", chocolateySources.Count());

        foreach (var chocolateySource in chocolateySources)
        {
            var nupkgFiles = GetFiles(BuildParameters.Paths.Directories.ChocolateyPackages + BuildParameters.ChocolateyNupkgGlobbingPattern);

            var chocolateyPushSettings = new ChocolateyPushSettings
                {
                    AllowUnofficial = true,
                    Source = chocolateySource.PushUrl
                };

            var canPushToChocolateySource = false;
            if (!string.IsNullOrEmpty(chocolateySource.Credentials.ApiKey))
            {
                context.Information("Setting ApiKey in Chocolatey Push Settings...");
                chocolateyPushSettings.ApiKey = chocolateySource.Credentials.ApiKey;
                canPushToChocolateySource = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(chocolateySource.Credentials.User) && !string.IsNullOrEmpty(chocolateySource.Credentials.Password))
                {
                    var chocolateySourceSettings = new ChocolateySourcesSettings
                    {
                        AllowUnofficial = true,
                        UserName = chocolateySource.Credentials.User,
                        Password = chocolateySource.Credentials.Password
                    };

                    context.Information("Adding Chocolatey source with user/pass...");
                    context.ChocolateyAddSource(isRelease ? "ReleaseSource" : "PreReleaseSource", chocolateySource.PushUrl, chocolateySourceSettings);
                    canPushToChocolateySource = true;
                }
                else
                {
                    context.Warning("User and Password are missing for {0} Chocolatey Source with Url {1}", isRelease ? "Release" : "PreRelease", chocolateySource.PushUrl);
                }
            }

            if (canPushToChocolateySource)
            {
                foreach (var nupkgFile in nupkgFiles)
                {
                    context.Information("Pushing {0} to {1} Source with Url {2}...", nupkgFile, isRelease ? "Release" : "PreRelease", chocolateySource.PushUrl);

                    // Push the package.
                    context.ChocolateyPush(nupkgFile, chocolateyPushSettings);
                }
            }
            else
            {
                context.Warning("Unable to push Chocolatey Packages to {0} Source with Url {1} as necessary credentials haven't been provided.", isRelease ? "Release" : "PreRelease", chocolateySource.PushUrl);
            }
        }
    }
    else
    {
        context.Information("Unable to publish Chocolatey packages. IsRunningOnWindows: {0} Chocolatey Packages Directory Exists: {0}", BuildParameters.BuildAgentOperatingSystem, DirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages));
    }
}

public void PushNuGetPackages(ICakeContext context, bool isRelease, List<PackageSourceData> nugetSources)
{
    if (DirectoryExists(BuildParameters.Paths.Directories.NuGetPackages))
    {
        context.Information("Number of configured {0} NuGet Sources: {1}", isRelease ? "Release" : "PreRelease", nugetSources.Count());

        foreach (var nugetSource in nugetSources)
        {
            var nupkgFiles = GetFiles(BuildParameters.Paths.Directories.NuGetPackages + BuildParameters.NuGetNupkgGlobbingPattern);

            var nugetPushSettings = new NuGetPushSettings
                {
                    Source = nugetSource.PushUrl
                };

            var canPushToNuGetSource = false;
            if (!string.IsNullOrEmpty(nugetSource.Credentials.ApiKey))
            {
                context.Information("Setting ApiKey in NuGet Push Settings...");
                nugetPushSettings.ApiKey = nugetSource.Credentials.ApiKey;
                canPushToNuGetSource = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(nugetSource.Credentials.User) && !string.IsNullOrEmpty(nugetSource.Credentials.Password))
                {
                    var nugetSourceSettings = new NuGetSourcesSettings
                        {
                            UserName = nugetSource.Credentials.User,
                            Password = nugetSource.Credentials.Password
                        };

                    context.Information("Adding NuGet source with user/pass...");
                    context.NuGetAddSource(isRelease ? "ReleaseSource" : "PreReleaseSource", nugetSource.PushUrl, nugetSourceSettings);
                    canPushToNuGetSource = true;
                }
                else
                {
                    context.Warning("User and Password are missing for {0} NuGet Source with Url {1}", isRelease ? "Release" : "PreRelease", nugetSource.PushUrl);
                }
            }

            if (canPushToNuGetSource)
            {
                foreach (var nupkgFile in nupkgFiles)
                {
                    context.Information("Pushing {0} to {1} Source with Url {2}...", nupkgFile, isRelease ? "Release" : "PreRelease", nugetSource.PushUrl);

                    // Push the package.
                    context.NuGetPush(nupkgFile, nugetPushSettings);
                }
            }
            else
            {
                 context.Warning("Unable to push NuGet Packages to {0} Source with Url {1} as necessary credentials haven't been provided.", isRelease ? "Release" : "PreRelease", nugetSource.PushUrl);
            }
        }
    }
    else
    {
        context.Information("Unable to publish NuGet Packages as NuGet Packages Directory doesn't exist.");
    }
}

BuildParameters.Tasks.PackageTask = Task("Package")
    .IsDependentOn("Export-Release-Notes")
    .Does(() => {
        foreach (var nuGetPackage in GetFiles(BuildParameters.Paths.Directories.NuGetPackages + BuildParameters.NuGetNupkgGlobbingPattern))
        {
            BuildParameters.BuildProvider.UploadArtifact(nuGetPackage);
        }

        foreach (var chocolateyPackage in GetFiles(BuildParameters.Paths.Directories.ChocolateyPackages + BuildParameters.ChocolateyNupkgGlobbingPattern))
        {
            BuildParameters.BuildProvider.UploadArtifact(chocolateyPackage);
        }
});