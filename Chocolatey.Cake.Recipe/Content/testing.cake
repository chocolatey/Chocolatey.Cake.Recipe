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

using System.Xml.Linq;
using System.Xml.XPath;

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Tuple<string, Exception> testsException = null;

BuildParameters.Tasks.TestNUnitTask = Task("Test-NUnit")
    .WithCriteria(() => BuildParameters.ShouldRunNUnit, "Skipping because NUnit is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => DirectoryExists(BuildParameters.Paths.Directories.PublishedNUnitTests), "Skipping because there are no published NUnit tests")
    .Does(() => RequireTool(ToolSettings.NUnitTool, () => {
        EnsureDirectoryExists(BuildParameters.Paths.Directories.NUnitTestResults);

        if (BuildParameters.TestExecutionType == "none")
        {
            Information("The TestExecutionType parameter has been set to 'none', so no tests will be executed");
            return;
        }

        var assembliesToTest = new FilePathCollection();

        if (BuildParameters.TestExecutionType == "unit")
        {
            Information("The TestExecutionType parameter has been set to 'unit'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedNUnitTests + BuildParameters.UnitTestAssemblyFilePattern);
        }
        else if (BuildParameters.TestExecutionType == "integration")
        {
            Information("The TestExecutionType parameter has been set to 'integration'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedNUnitTests + BuildParameters.IntegrationTestAssemblyFilePattern);
        }
        else if (BuildParameters.TestExecutionType == "all")
        {
            Information("The TestExecutionType parameter has been set to 'all'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedNUnitTests + BuildParameters.UnitTestAssemblyFilePattern)
                            + GetFiles(BuildParameters.Paths.Directories.PublishedNUnitTests + BuildParameters.IntegrationTestAssemblyFilePattern);
        }

        if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows && BuildParameters.ShouldRunOpenCover)
        {
            Information("Running OpenCover and NUnit...");

            RequireTool(ToolSettings.OpenCoverTool, () => {
                OpenCover(tool => {
                    tool.NUnit3(assembliesToTest, new NUnit3Settings {
                        Work = BuildParameters.Paths.Directories.NUnitTestResults
                    });
                },
                BuildParameters.Paths.Files.TestCoverageOutputFilePath,
                new OpenCoverSettings
                {
                    OldStyle = true,
                    ReturnTargetCodeOffset = 0
                }
                    .WithFilter(ToolSettings.TestCoverageFilter)
                    .ExcludeByAttribute(ToolSettings.TestCoverageExcludeByAttribute)
                    .ExcludeByFile(ToolSettings.TestCoverageExcludeByFile));
            });
        }
        else
        {
            Information("Running NUnit...");

            // OpenCover doesn't work on anything non-windows, so let's just run NUnit by itself
            NUnit3(assembliesToTest, new NUnit3Settings {
                Work = BuildParameters.Paths.Directories.NUnitTestResults
            });
        }
    })
).OnError((exception) =>
{
    Warning("Task Test-NUnit failed with the exception message {0}", exception.Message);
    testsException = new Tuple<string, Exception>("Test-NUnit", exception);
});

BuildParameters.Tasks.TestxUnitTask = Task("Test-xUnit")
    .WithCriteria(() => BuildParameters.ShouldRunxUnit, "Skipping because xUnit is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => DirectoryExists(BuildParameters.Paths.Directories.PublishedxUnitTests), "Skipping because there are no published xUnit tests")
    .Does(() => RequireTool(ToolSettings.XUnitTool, () => {
        EnsureDirectoryExists(BuildParameters.Paths.Directories.xUnitTestResults);

        if (BuildParameters.TestExecutionType == "none")
        {
            Information("The TestExecutionType parameter has been set to 'none', so no tests will be executed");
            return;
        }

        var assembliesToTest = new FilePathCollection();

        if (BuildParameters.TestExecutionType == "unit")
        {
            Information("The TestExecutionType parameter has been set to 'unit'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedxUnitTests + BuildParameters.UnitTestAssemblyFilePattern);
        }
        else if (BuildParameters.TestExecutionType == "integration")
        {
            Information("The TestExecutionType parameter has been set to 'integration'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedxUnitTests + BuildParameters.IntegrationTestAssemblyFilePattern);
        }
        else if (BuildParameters.TestExecutionType == "all")
        {
            Information("The TestExecutionType parameter has been set to 'all'");

            assembliesToTest = GetFiles(BuildParameters.Paths.Directories.PublishedxUnitTests + BuildParameters.UnitTestAssemblyFilePattern)
                            + GetFiles(BuildParameters.Paths.Directories.PublishedxUnitTests + BuildParameters.IntegrationTestAssemblyFilePattern);
        }

        if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows && BuildParameters.ShouldRunOpenCover)
        {
            Information("Running OpenCover and xUnit...");

            RequireTool(ToolSettings.OpenCoverTool, () => {
                OpenCover(tool => {
                    tool.XUnit2(assembliesToTest, new XUnit2Settings {
                        OutputDirectory = BuildParameters.Paths.Directories.xUnitTestResults,
                        XmlReport = true,
                        NoAppDomain = true
                    });
                },
                BuildParameters.Paths.Files.TestCoverageOutputFilePath,
                new OpenCoverSettings
                {
                    OldStyle = true,
                    ReturnTargetCodeOffset = 0
                }
                    .WithFilter(ToolSettings.TestCoverageFilter)
                    .ExcludeByAttribute(ToolSettings.TestCoverageExcludeByAttribute)
                    .ExcludeByFile(ToolSettings.TestCoverageExcludeByFile));
            });
        }
        else
        {
            Information("Running xUnit...");

            // OpenCover doesn't work on anything non-windows, so let's just run xUnit by itself
            XUnit2(assembliesToTest, new XUnit2Settings {
                OutputDirectory = BuildParameters.Paths.Directories.xUnitTestResults,
                XmlReport = true,
                NoAppDomain = true
            });
        }
    })
).OnError((exception) =>
{
    Warning("Task Test-xUnit failed with the exception message {0}", exception.Message);
    testsException = new Tuple<string, Exception>("Test-xUnit", exception);
});

BuildParameters.Tasks.DotNetTestTask = Task("DotNetTest")
    .WithCriteria(() => BuildParameters.ShouldRunDotNetTest, "Skipping because dotnet test is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .Does(() => {
        if (BuildParameters.TestExecutionType == "none")
        {
            Information("The TestExecutionType parameter has been set to 'none', so no tests will be executed");
            return;
        }

        var msBuildSettings = new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", BuildParameters.Version.SemVersion)
                                .WithProperty("AssemblyVersion", BuildParameters.Version.FileVersion)
                                .WithProperty("FileVersion",  BuildParameters.Version.FileVersion)
                                .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

        if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
        {
            var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

            // Use FrameworkPathOverride when not running on Windows.
            Information("Restore will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
            msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
        }

        var projectsToTest = new FilePathCollection();

        if (BuildParameters.TestExecutionType == "unit")
        {
            Information("The TestExecutionType parameter has been set to 'unit'");
            projectsToTest = GetFiles(BuildParameters.TestDirectoryPath + BuildParameters.UnitTestAssemblyProjectPattern);
        }
        else if (BuildParameters.TestExecutionType == "integration")
        {
            Information("The TestExecutionType parameter has been set to 'integration'");
            projectsToTest = GetFiles(BuildParameters.TestDirectoryPath + BuildParameters.IntegrationTestAssemblyProjectPattern);
        }
        else if (BuildParameters.TestExecutionType == "all")
        {
            Information("The TestExecutionType parameter has been set to 'all'");
            projectsToTest = GetFiles(BuildParameters.TestDirectoryPath + BuildParameters.UnitTestAssemblyProjectPattern)
                            + GetFiles(BuildParameters.TestDirectoryPath + BuildParameters.IntegrationTestAssemblyProjectPattern);
        }

        // We create the coverlet settings here so we don't have to create the filters several times
        var coverletSettings = new CoverletSettings
        {
            CollectCoverage         = true,
            // It is problematic to merge the reports into one, as such we use a custom directory for coverage results
            CoverletOutputDirectory = BuildParameters.Paths.Directories.TestCoverage.Combine("coverlet"),
            CoverletOutputFormat    = CoverletOutputFormat.opencover,
            ExcludeByFile           = ToolSettings.TestCoverageExcludeByFile.Split(new [] {';' }, StringSplitOptions.None).ToList(),
            ExcludeByAttribute      = ToolSettings.TestCoverageExcludeByAttribute.Split(new [] {';' }, StringSplitOptions.None).ToList()
        };

        foreach (var filter in ToolSettings.TestCoverageFilter.Split(new [] {' ' }, StringSplitOptions.None))
        {
            if (filter[0] == '+')
            {
                coverletSettings.WithInclusion(filter.TrimStart('+'));
            }
            else if (filter[0] == '-')
            {
                coverletSettings.WithFilter(filter.TrimStart('-'));
            }
        }

        var settings = new DotNetCoreTestSettings
        {
            Configuration = BuildParameters.Configuration,
            NoBuild = true
        };

        foreach (var project in projectsToTest)
        {
            Information("Inspecting Unit Test configuration for project: {0}", project);
            Action<ICakeContext> testAction = tool =>
            {
                tool.DotNetCoreTest(project.FullPath, settings);
            };

            var parsedProject = ParseProject(project, BuildParameters.Configuration);
            var coverletPackage = parsedProject.GetPackage("coverlet.msbuild");
            bool shouldAddSourceLinkArgument = false; // Set it to false by default due to OpenCover
            
            if (coverletPackage != null)
            {
                // If the version is a pre-release, we will assume that it is a later
                // version than what we need, and thus TryParse will return false.
                // If TryParse is successful we need to compare the coverlet version
                // to ensure it is higher or equal to the version that includes the fix
                // for using the SourceLink argument.
                // https://github.com/coverlet-coverage/coverlet/issues/882
                Version coverletVersion;
                shouldAddSourceLinkArgument = !Version.TryParse(coverletPackage.Version, out coverletVersion)
                    || coverletVersion >= Version.Parse("2.9.1");
            }

            settings.ArgumentCustomization = args => {
                args.AppendMSBuildSettings(msBuildSettings, Context.Environment);
                if (shouldAddSourceLinkArgument && parsedProject.HasPackage("Microsoft.SourceLink.GitHub"))
                {
                    args.Append("/p:UseSourceLink=true");
                }
                return args;
            };

            if (coverletPackage != null)
            {
                Information("Running DotNetCoreTest along with Coverlet...");

                coverletSettings.CoverletOutputName = parsedProject.RootNameSpace.Replace('.', '-');
                DotNetCoreTest(project.FullPath, settings, coverletSettings);
            }
            else if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows || !BuildParameters.ShouldRunOpenCover)
            {
                Information("Invoking Test Action..");

                testAction(Context);
            }
            else
            {
                Information("Running OpenCover...");

                // We can not use msbuild properties together with opencover
                settings.ArgumentCustomization = null;
                
                RequireTool(ToolSettings.OpenCoverTool, () => {
                    OpenCover(testAction,
                        BuildParameters.Paths.Files.TestCoverageOutputFilePath,
                        new OpenCoverSettings {
                            ReturnTargetCodeOffset = 0,
                            OldStyle = true,
                            Register = "user",
                            MergeOutput = FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath)
                        }
                        .WithFilter(ToolSettings.TestCoverageFilter)
                        .ExcludeByAttribute(ToolSettings.TestCoverageExcludeByAttribute)
                        .ExcludeByFile(ToolSettings.TestCoverageExcludeByFile));
                });
            }
        }
}).OnError((exception) =>
{
    Warning("Task DotNetTest failed with the exception message {0}", exception.Message);
    testsException = new Tuple<string, Exception>("DotNetTest", exception);
});

BuildParameters.Tasks.GenerateFriendlyTestReportTask = Task("Generate-FriendlyTestReport")
    .IsDependentOn("Test-NUnit")
    .IsDependentOn("Test-xUnit")
    .WithCriteria(() => BuildParameters.ShouldRunReportUnit, "Skipping because ReportUnit is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .ContinueOnError() // We do not want this task to fail the build
    .Does(() => RequireTool(ToolSettings.ReportUnitTool, () =>
    {
        var possibleDirectories = new[] {
            BuildParameters.Paths.Directories.xUnitTestResults,
            BuildParameters.Paths.Directories.NUnitTestResults,
        };

        foreach (var directory in possibleDirectories.Where((d) => DirectoryExists(d)))
        {
            Information("Generating Friendly Test Report from directory: {0}", directory);

            ReportUnit(directory, directory, new ReportUnitSettings());

            var reportUnitFiles = GetFiles(directory + "/*.html");
            var reportUnitZipFileName = directory.FullPath.Contains("xunit") ? "xunit-reportunit.zip" : "nunit-reportunit.zip";
            var rootPath =directory.FullPath.Contains("xunit") ? "./code_drop/TestResults/xUnit" : "./code_drop/TestResults/NUnit";
            Zip(rootPath, directory + "/" + reportUnitZipFileName, reportUnitFiles);

            BuildParameters.BuildProvider.UploadArtifact(directory + "/" + reportUnitZipFileName);
        }
    })
);

BuildParameters.Tasks.ReportUnitTestResultsTask = Task("Report-UnitTestResults")
    .WithCriteria(() => BuildParameters.ShouldReportUnitTestResults, "Skipping because reporting of unit test results is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity, "Skipping due to not running on TeamCity")
    .ContinueOnError() // We do not want this task to fail the build
    .Does(() => {
        Information("Reporting Unit Test results to TeamCity if any exist...");
        foreach (var xUnitResultFile in GetFiles(BuildParameters.Paths.Directories.xUnitTestResults + "/*.xml"))
        {
            Information("Reporting XUnit file: {0}", xUnitResultFile);
            TeamCity.ImportData("xunit", xUnitResultFile);
        }

        foreach (var nUnitResultFile in GetFiles(BuildParameters.Paths.Directories.NUnitTestResults + "/*.xml"))
        {
            Information("Reporting NUnit file: {0}", nUnitResultFile);
            TeamCity.ImportData("nunit", nUnitResultFile);
        }
});

BuildParameters.Tasks.ReportCodeCoverageMetricsTask = Task("Report-Code-Coverage-Metrics")
    .IsDependentOn("Convert-OpenCoverToLcov")
    .WithCriteria(() => BuildParameters.ShouldReportCodeCoverageMetrics, "Skipping because reporting of code coverage metrics is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => BuildSystem.IsRunningOnTeamCity, "Skipping due to not running on TeamCity")
    .ContinueOnError() // We do not want this task to fail the build
    .Does(() => {
        var coverageFiles = GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/coverlet/*.xml");
        if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath))
        {
            coverageFiles += BuildParameters.Paths.Files.TestCoverageOutputFilePath;
        }

        double totalVisitedClasses = 0.0;
        double totalClasses = 0.0;
        double totalVisitedMethods = 0.0;
        double totalMethods = 0.0;
        double totalVisitedSequencePoints = 0.0;
        double totalSequencePoints = 0.0;
        double totalVisitedBranchPoints = 0.0;
        double totalBranchPoints = 0.0;

        foreach(var coverageFile in coverageFiles)
        {
            XDocument doc = XDocument.Load(coverageFile.FullPath);
            XElement summary = doc.XPathSelectElement("/CoverageSession/Summary");

            totalVisitedClasses += Convert.ToDouble(summary.Attribute("visitedClasses").Value);
            totalClasses += Convert.ToDouble(summary.Attribute("numClasses").Value);
            totalVisitedMethods += Convert.ToDouble(summary.Attribute("visitedMethods").Value);
            totalMethods += Convert.ToDouble(summary.Attribute("numMethods").Value);
            totalVisitedSequencePoints += Convert.ToDouble(summary.Attribute("visitedSequencePoints").Value);
            totalSequencePoints += Convert.ToDouble(summary.Attribute("numSequencePoints").Value);
            totalVisitedBranchPoints += Convert.ToDouble(summary.Attribute("visitedBranchPoints").Value);
            totalBranchPoints += Convert.ToDouble(summary.Attribute("numBranchPoints").Value);
        }

        // Classes.
        ReportCoverageMetric(
            totalVisitedClasses,
            totalClasses,
            "CodeCoverageAbsCCovered",
            "CodeCoverageAbsCTotal",
            "CodeCoverageC");

        // Methods.
        ReportCoverageMetric(
            totalVisitedMethods,
            totalMethods,
            "CodeCoverageAbsMCovered",
            "CodeCoverageAbsMTotal",
            "CodeCoverageM");

        // Sequence points / statements.
        ReportCoverageMetric(
            totalVisitedSequencePoints,
            totalSequencePoints,
            "CodeCoverageAbsSCovered",
            "CodeCoverageAbsSTotal",
            "CodeCoverageS");

        // Branches.
        ReportCoverageMetric(
            totalVisitedBranchPoints,
            totalBranchPoints,
            "CodeCoverageAbsBCovered",
            "CodeCoverageAbsBTotal",
            "CodeCoverageB");
});

private void ReportCoverageMetric(
    double totalVisited,
    double total,
    string tcVisitedKey,
    string tcTotalKey,
    string tcCoverageKey)
{
    double coverage = (totalVisited / total) * 100;

    Information($"##teamcity[buildStatisticValue key='{tcVisitedKey}' value='{totalVisited}']");
    Information($"##teamcity[buildStatisticValue key='{tcTotalKey}' value='{total}']");
    Information($"##teamcity[buildStatisticValue key='{tcCoverageKey}' value='{coverage}']");
}

BuildParameters.Tasks.GenerateLocalCoverageReportTask = Task("Generate-FriendlyCoverageReport")
    .WithCriteria(() => BuildParameters.ShouldRunReportGenerator, "Skipping because ReportGenarator is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .ContinueOnError() // We do not want this task to fail the build
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.ReportGeneratorGlobalTool : ToolSettings.ReportGeneratorTool, () => {
        var coverageFiles = GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/coverlet/*.xml");
        if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath))
        {
            coverageFiles += BuildParameters.Paths.Files.TestCoverageOutputFilePath;
        }

        if (coverageFiles.Any())
        {
            Information("Generating Friendly Coverage Report...");

            var settings = new ReportGeneratorSettings();
            if (BuildParameters.IsDotNetBuild && BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
            {
                // Workaround until 0.38.5+ version of cake is released
                // https://github.com/cake-build/cake/pull/2824
                settings.ToolPath = Context.Tools.Resolve("reportgenerator");
            }

            ReportGenerator(coverageFiles, BuildParameters.Paths.Directories.TestCoverage, settings);

            var reportGeneratorFiles = GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/*.html")
                                     + GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/*.htm")
                                     + GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/*.js")
                                     + GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/*.svg")
                                     + GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/*.css");

            var reportGeneratorZipFileName = "coverage.zip";
            Zip("./code_drop/TestCoverage", BuildParameters.Paths.Directories.TestCoverage + "/" + reportGeneratorZipFileName, reportGeneratorFiles);

            BuildParameters.BuildProvider.UploadArtifact(BuildParameters.Paths.Directories.TestCoverage + "/" + reportGeneratorZipFileName);
        }
        else
        {
            Warning("No coverage files was found, no local report is generated!");
        }
    })
);

BuildParameters.Tasks.GenerateLocalCoverageReportTask = Task("Convert-OpenCoverToLcov")
    .WithCriteria(() => BuildParameters.ShouldRunReportGenerator, "Skipping because ReportGenarator is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .ContinueOnError() // We do not want this task to fail the build
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.ReportGeneratorGlobalTool : ToolSettings.ReportGeneratorTool, () => {
        if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath))
        {
            Information("Converting OpenCover TestCoverage file to lcov format...");

            var settings = new ReportGeneratorSettings();

            // Workaround until 0.38.5+ version of Cake is used in the Recipe
            settings.ArgumentCustomization = args => args.Append("-reporttypes:lcov");

            if (BuildParameters.IsDotNetBuild && BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
            {
                // Workaround until 0.38.5+ version of cake is released
                // https://github.com/cake-build/cake/pull/2824
                settings.ToolPath = Context.Tools.Resolve("reportgenerator");
            }

            ReportGenerator(BuildParameters.Paths.Files.TestCoverageOutputFilePath, BuildParameters.Paths.Directories.TestCoverage, settings);
        }
        else
        {
            Warning("No coverage files was found, no local report is generated!");
        }
    })
);

BuildParameters.Tasks.TestTask = Task("Test")
    .WithCriteria(() => BuildParameters.ShouldRunTests, "Skipping because running tests is not enabled")
    .Does(() => {
        var coverageFiles = GetFiles(BuildParameters.Paths.Directories.TestCoverage + "/coverlet/*.xml");
        if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath))
        {
            coverageFiles += BuildParameters.Paths.Files.TestCoverageOutputFilePath;
        }

        foreach (var coverageFile in coverageFiles)
        {
            Information("Uploading Test Coverage file: {0}", coverageFile);
            BuildParameters.BuildProvider.UploadArtifact(coverageFile);
        }

        foreach (var nUnitResultFile in GetFiles(BuildParameters.Paths.Directories.NUnitTestResults + "/*.xml"))
        {
            Information("Uploading NUnit Test Result file: {0}", nUnitResultFile);
            BuildParameters.BuildProvider.UploadArtifact(nUnitResultFile);
        }

        foreach (var xUnitResultFile in GetFiles(BuildParameters.Paths.Directories.xUnitTestResults + "/*.xml"))
        {
            Information("Uploading xUnit Test Result file: {0}", xUnitResultFile);
            BuildParameters.BuildProvider.UploadArtifact(xUnitResultFile);
        }

        if (FileExists(BuildParameters.Paths.Directories.TestCoverage + "/lcov.info"))
        {
            Information("Uploading lcov.info Test Coverage Report...");
            BuildParameters.BuildProvider.UploadArtifact(BuildParameters.Paths.Directories.TestCoverage + "/lcov.info");
        }

        // Let us check and rethrow any exception that occurred while
        // running the unit tests.
        if (testsException != null)
        {
            Error("The Task {0} failed!", testsException.Item1);
            throw testsException.Item2;
        }
});