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

public class BuildPaths
{
    public BuildFiles Files { get; private set; }
    public BuildDirectories Directories { get; private set; }

    public static BuildPaths GetPaths()
    {
        // Directories
        var buildDirectoryPath             = "./code_drop";
        var tempBuildDirectoryPath         = buildDirectoryPath + "/temp";
        var publishedNUnitTestsDirectory   = tempBuildDirectoryPath + "/_PublishedNUnitTests";
        var publishedxUnitTestsDirectory   = tempBuildDirectoryPath + "/_PublishedxUnitTests";
        var publishedWebsitesDirectory     = tempBuildDirectoryPath + "/_PublishedWebsites";
        var publishedApplicationsDirectory = tempBuildDirectoryPath + "/_PublishedApps";
        var publishedLibrariesDirectory    = tempBuildDirectoryPath + "/_PublishedLibs";
        var publishedLambdasDirectory      = tempBuildDirectoryPath + "/_PublishedLambdas";
        var tempNuspecDirectory            = tempBuildDirectoryPath + "/nuspec";
        var publishedDocumentationDirectory= buildDirectoryPath + "/Documentation";
        var environmentSettingsDirectory   = "./settings";

        var chocolateyNuspecDirectory = tempNuspecDirectory + "/chocolatey";
        var nugetNuspecDirectory = tempNuspecDirectory + "/nuget";

        var testResultsDirectory = buildDirectoryPath + "/TestResults";
        var inspectCodeResultsDirectory = testResultsDirectory + "/InspectCode";
        var scriptAnalyzerResultsDirectory = testResultsDirectory + "/PSScriptAnalyzer";
        var NUnitTestResultsDirectory = testResultsDirectory + "/NUnit";
        var xUnitTestResultsDirectory = testResultsDirectory + "/xUnit";

        var testCoverageDirectory = buildDirectoryPath + "/TestCoverage";

        var packagesDirectory = buildDirectoryPath + "/Packages";
        var nuGetPackagesOutputDirectory = packagesDirectory + "/NuGet";
        var chocolateyPackagesOutputDirectory = packagesDirectory + "/Chocolatey";

        var dependencyCheckReportsDirectory = buildDirectoryPath + "/DependencyCheckReports";
        
        var signedFilesDirectory = buildDirectoryPath + "/SignedFiles";

        // Files
        var dotNetFormatOutputFilePath = ((DirectoryPath)testResultsDirectory).CombineWithFilePath("dotnet-format.json");
        var testCoverageOutputFilePath = ((DirectoryPath)testCoverageDirectory).CombineWithFilePath("OpenCover.xml");
        var solutionInfoFilePath = ((DirectoryPath)BuildParameters.SourceDirectoryPath).CombineWithFilePath("SolutionVersion.cs");
        var buildLogFilePath = ((DirectoryPath)buildDirectoryPath).CombineWithFilePath("MsBuild.log");
        var dependencyCheckJsonReportFilePath = ((DirectoryPath)dependencyCheckReportsDirectory).CombineWithFilePath("dependency-check-report.json");
        var dependencyCheckHtmlReportFilePath = ((DirectoryPath)dependencyCheckReportsDirectory).CombineWithFilePath("dependency-check-report.html");

        var repoFilesPaths = new FilePath[] {
            "LICENSE",
            "README.md"
        };

        var buildDirectories = new BuildDirectories(
            buildDirectoryPath,
            tempBuildDirectoryPath,
            publishedNUnitTestsDirectory,
            publishedxUnitTestsDirectory,
            publishedWebsitesDirectory,
            publishedApplicationsDirectory,
            publishedLibrariesDirectory,
            publishedLambdasDirectory,
            chocolateyNuspecDirectory,
            nugetNuspecDirectory,
            testResultsDirectory,
            inspectCodeResultsDirectory,
            scriptAnalyzerResultsDirectory,
            NUnitTestResultsDirectory,
            xUnitTestResultsDirectory,
            testCoverageDirectory,
            nuGetPackagesOutputDirectory,
            chocolateyPackagesOutputDirectory,
            packagesDirectory,
            environmentSettingsDirectory,
            dependencyCheckReportsDirectory,
            signedFilesDirectory
            );

        var buildFiles = new BuildFiles(
            repoFilesPaths,
            dotNetFormatOutputFilePath,
            testCoverageOutputFilePath,
            solutionInfoFilePath,
            buildLogFilePath,
            dependencyCheckJsonReportFilePath,
            dependencyCheckHtmlReportFilePath
            );

        return new BuildPaths
        {
            Files = buildFiles,
            Directories = buildDirectories
        };
    }
}

public class BuildFiles
{
    public ICollection<FilePath> RepoFilesPaths { get; private set; }

    public FilePath DotNetFormatOutputFilePath { get; private set; }
    public FilePath TestCoverageOutputFilePath { get; private set; }

    public FilePath SolutionInfoFilePath { get; private set; }

    public FilePath BuildLogFilePath { get; private set; }

    public FilePath DependencyCheckJsonReportFilePath { get; private set; }

    public FilePath DependencyCheckHtmlReportFilePath { get; private set; }

    public BuildFiles(
        FilePath[] repoFilesPaths,
        FilePath dotNetFormatOutputFilePath,
        FilePath testCoverageOutputFilePath,
        FilePath solutionInfoFilePath,
        FilePath buildLogFilePath,
        FilePath dependencyCheckJsonReportFilePath,
        FilePath dependencyCheckHtmlReportFilePath
        )
    {
        RepoFilesPaths = Filter(repoFilesPaths);
        DotNetFormatOutputFilePath = dotNetFormatOutputFilePath;
        TestCoverageOutputFilePath = testCoverageOutputFilePath;
        SolutionInfoFilePath = solutionInfoFilePath;
        BuildLogFilePath = buildLogFilePath;
        DependencyCheckJsonReportFilePath = dependencyCheckJsonReportFilePath;
        DependencyCheckHtmlReportFilePath = dependencyCheckHtmlReportFilePath;
    }

    private static FilePath[] Filter(FilePath[] files)
    {
        // Not a perfect solution, but we need to filter PDB files
        // when building on an OS that's not Windows (since they don't exist there).
        if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
        {
            return files.Where(f => !f.FullPath.EndsWith("pdb")).ToArray();
        }

        return files;
    }
}

public class BuildDirectories
{
    public DirectoryPath Build { get; private set; }
    public DirectoryPath TempBuild { get; private set; }
    public DirectoryPath PublishedNUnitTests { get; private set; }
    public DirectoryPath PublishedxUnitTests { get; private set; }
    public DirectoryPath PublishedWebsites { get; private set; }
    public DirectoryPath PublishedApplications { get; private set; }
    public DirectoryPath PublishedLibraries { get; private set; }
    public DirectoryPath PublishedLambdas { get; private set; }
    public DirectoryPath PublishedDocumentation { get; private set; }
    public DirectoryPath ChocolateyNuspecDirectory { get; private set; }
    public DirectoryPath NuGetNuspecDirectory { get; private set; }
    public DirectoryPath TestResults { get; private set; }
    public DirectoryPath InspectCodeTestResults { get; private set; }
    public DirectoryPath PSScriptAnalyzerResults { get; private set; }
    public DirectoryPath NUnitTestResults { get; private set; }
    public DirectoryPath xUnitTestResults { get; private set; }
    public DirectoryPath TestCoverage { get; private set; }
    public DirectoryPath NuGetPackages { get; private set; }
    public DirectoryPath ChocolateyPackages { get; private set; }
    public DirectoryPath Packages { get; private set; }
    public DirectoryPath EnvironmentSettings { get; private set; }
    public DirectoryPath DependencyCheckReports { get; private set; }
    public DirectoryPath SignedFiles { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath build,
        DirectoryPath tempBuild,
        DirectoryPath publishedNUnitTests,
        DirectoryPath publishedxUnitTests,
        DirectoryPath publishedWebsites,
        DirectoryPath publishedApplications,
        DirectoryPath publishedLibraries,
        DirectoryPath publishedLambdas,
        DirectoryPath chocolateyNuspecDirectory,
        DirectoryPath nugetNuspecDirectory,
        DirectoryPath testResults,
        DirectoryPath inspectCodeTestResults,
        DirectoryPath scriptAnalyzerResults,
        DirectoryPath nunitTestResults,
        DirectoryPath xunitTestResults,
        DirectoryPath testCoverage,
        DirectoryPath nuGetPackages,
        DirectoryPath chocolateyPackages,
        DirectoryPath packages,
        DirectoryPath environmentSettings,
        DirectoryPath dependencyCheckReports,
        DirectoryPath signedFiles
        )
    {
        Build = build;
        TempBuild = tempBuild;
        PublishedNUnitTests = publishedNUnitTests;
        PublishedxUnitTests = publishedxUnitTests;
        PublishedWebsites = publishedWebsites;
        PublishedApplications = publishedApplications;
        PublishedLibraries = publishedLibraries;
        PublishedLambdas = publishedLambdas;
        ChocolateyNuspecDirectory = chocolateyNuspecDirectory;
        NuGetNuspecDirectory = nugetNuspecDirectory;
        TestResults = testResults;
        InspectCodeTestResults = inspectCodeTestResults;
        PSScriptAnalyzerResults = scriptAnalyzerResults;
        NUnitTestResults = nunitTestResults;
        xUnitTestResults = xunitTestResults;
        TestCoverage = testCoverage;
        NuGetPackages = nuGetPackages;
        ChocolateyPackages = chocolateyPackages;
        EnvironmentSettings = environmentSettings;
        DependencyCheckReports = dependencyCheckReports;
        SignedFiles = signedFiles;
        Packages = packages;

        ToClean = new[] {
            Build,
            TempBuild,
            TestResults,
            TestCoverage.Combine("coverlet"),
            TestCoverage
        };
    }
}
