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

public static class ToolSettings
{
    static ToolSettings()
    {
        SetToolPreprocessorDirectives();
    }

    public static MSBuildToolVersion BuildMSBuildToolVersion { get; private set; }
    public static FilePath MSBuildToolPath { get; private set; }
    public static PlatformTarget BuildPlatformTarget { get; private set; }
    public static string XBuildPlatformTarget { get; private set; }
    public static FilePath EazfuscatorToolLocation { get; private set; }
    public static string AmazonLambdaGlobalTool { get; private set; }
    public static string DependencyCheckTool { get; private set; }
    public static bool DependencyCheckDisableYarnAudit { get; private set; }
    public static string DotNetFormatGlobalTool { get; private set; }
    public static string GitVersionGlobalTool { get; private set; }
    public static string GitVersionTool { get; private set; }
    public static string GitReleaseManagerGlobalTool { get; private set; }
    public static string GitReleaseManagerTool { get; private set; }
    public static string ILMergeTool { get; private set; }
    public static int MaxCpuCount { get; private set; }
    public static string MSBuildExtensionPackTool { get; private set; }
    public static string NUnitTool { get; private set; }
    public static string OpenCoverTool { get; private set; }
    public static DirectoryPath OutputDirectory { get; private set; }
    public static string ReportGeneratorGlobalTool { get; private set; }
    public static string ReportGeneratorTool { get; private set; }
    public static string ReportUnitTool { get; private set; }
    public static string ReSharperReportsTool { get; private set; }
    public static string ReSharperTools { get; private set; }
    public static List<string> PSScriptAnalyzerExcludePaths { get; private set; }
    public static string SonarQubeTool { get; private set; }
    public static string StrongNameSignerTool { get; private set; }
    public static string TestCoverageExcludeByAttribute { get; private set; }
    public static string TestCoverageExcludeByFile { get; private set; }
    public static string TestCoverageFilter { get; private set; }
    public static string WixTool { get; private set; }
    public static string XUnitTool { get; private set; }

    public static void SetToolPreprocessorDirectives(
        string amazonLambdaGlobalTool = "#tool dotnet:?package=amazon.lambda.tools&version=5.4.5",
        string dependencyCheckTool = "#tool nuget:?package=DependencyCheck.Runner.Tool&version=3.2.1&include=./**/dependency-check.sh&include=./**/dependency-check.bat",
        string dotNetFormatGlobalTool = "#tool dotnet:?package=dotnet-format&version=5.1.250801",
        string gitVersionGlobalTool = "#tool dotnet:?package=GitVersion.Tool&version=5.10.1",
        string gitVersionTool = "#tool nuget:?package=GitVersion.CommandLine&version=5.10.1",
        string gitReleaseManagerGlobalTool = "#tool dotnet:?package=GitReleaseManager.Tool&version=0.20.0",
        string gitReleaseManagerTool = "#tool nuget:?package=GitReleaseManager&version=0.20.0",
        string ilMergeTool = "#tool nuget:?package=ilmerge&version=3.0.41",
        string msbuildExtensionPackTool = "#tool nuget:?package=MSBuild.Extension.Pack&version=1.9.0",
        string nunitTool = "#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0",
        string openCoverTool = "#tool nuget:?package=OpenCover&version=4.7.1221",
        string reportGeneratorGlobalTool = "#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.8.5",
        string reportGeneratorTool = "#tool nuget:?package=ReportGenerator&version=5.1.6",
        string reportUnitTool = "#tool nuget:?package=ReportUnit&version=1.2.1",
        string reSharperReportsTool = "#tool nuget:?package=ReSharperReports&version=0.2.0",
        string reSharperTools = "#tool nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2017.2.0",
        string sonarQubeTool = "#tool nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.8.0",
        string strongNameSignerTool = "#tool nuget:?package=Brutal.Dev.StrongNameSigner&version=2.6.0",
        string wixTool = "#tool nuget:?package=WiX&version=3.11.2",
        string xunitTool = "#tool nuget:?package=xunit.runner.console&version=2.4.1"
    )
    {
        AmazonLambdaGlobalTool = amazonLambdaGlobalTool;
        DependencyCheckTool = dependencyCheckTool;
        DotNetFormatGlobalTool = dotNetFormatGlobalTool;
        GitVersionGlobalTool = gitVersionGlobalTool;
        GitVersionTool = gitVersionTool;
        GitReleaseManagerGlobalTool = gitReleaseManagerGlobalTool;
        GitReleaseManagerTool = gitReleaseManagerTool;
        ILMergeTool = ilMergeTool;
        MSBuildExtensionPackTool = msbuildExtensionPackTool;
        NUnitTool = nunitTool;
        OpenCoverTool = openCoverTool;
        ReportGeneratorGlobalTool = reportGeneratorGlobalTool;
        ReportGeneratorTool = reportGeneratorTool;
        ReportUnitTool = reportUnitTool;
        ReSharperReportsTool = reSharperReportsTool;
        ReSharperTools = reSharperTools;
        SonarQubeTool = sonarQubeTool;
        StrongNameSignerTool = strongNameSignerTool;
        WixTool = wixTool;
        XUnitTool = xunitTool;
    }

    public static void SetToolSettings(
        ICakeContext context,
        PlatformTarget? buildPlatformTarget = null,
        string xBuildPlatformTarget = "Any CPU",
        MSBuildToolVersion buildMSBuildToolVersion = MSBuildToolVersion.Default,
        FilePath msBuildToolPath = null,
        FilePath eazfuscatorToolLocation = null,
        int? maxCpuCount = null,
        DirectoryPath outputDirectory = null,
        List<string> scriptAnalyzerExcludePaths = null,
        string testCoverageExcludeByAttribute = null,
        string testCoverageExcludeByFile = null,
        string testCoverageFilter = null,
        bool dependencyCheckDisableYarnAudit = false
    )
    {
        context.Information("Setting up tools...");

        BuildPlatformTarget = buildPlatformTarget ?? PlatformTarget.MSIL;
        XBuildPlatformTarget = xBuildPlatformTarget;
        BuildMSBuildToolVersion = buildMSBuildToolVersion;
        EazfuscatorToolLocation = eazfuscatorToolLocation ?? "./lib/Eazfuscator.NET/Eazfuscator.NET.exe";
        MaxCpuCount = maxCpuCount ?? 0;
        OutputDirectory = outputDirectory;
        PSScriptAnalyzerExcludePaths = scriptAnalyzerExcludePaths ?? new List<String> { "tools",  "code_drop", @"src\*\bin\Debug", @"Source\*\bin\Debug", @"src\*\bin\Release", @"Source\*\bin\Release", @"src\packages", @"Source\packages" };
        TestCoverageExcludeByAttribute = testCoverageExcludeByAttribute ?? "*.ExcludeFromCodeCoverage*";
        TestCoverageExcludeByFile = testCoverageExcludeByFile ?? "*/*Designer.cs;*/*.g.cs;*/*.g.i.cs";
        TestCoverageFilter = testCoverageFilter ?? string.Format("+[{0}*]* +[{1}*]* -[*.tests]* -[*.Tests]*", BuildParameters.Title, BuildParameters.Title.ToLowerInvariant());

        DependencyCheckDisableYarnAudit = dependencyCheckDisableYarnAudit;
        
        if (context.HasArgument("dependencyCheckDisableYarnAudit"))
        {
            DependencyCheckDisableYarnAudit = context.Argument<bool>("dependencyCheckDisableYarnAudit");
        }

        // We only use MSBuild when running on Windows. Elsewhere, we use XBuild when required. As a result,
        // we only need to detect the correct version of MSBuild when running on WIndows, and when it hasn't
        // been explicitly set.
        if (msBuildToolPath == null && BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows && !BuildParameters.Target.StartsWith("Docker"))
        {
            msBuildToolPath = ResolveVisualStudioMsBuildPath(context, BuildMSBuildToolVersion);

            if (msBuildToolPath == null)
            {
                context.Warning("No supported MSBuild Tool was found! Continuing with automatic detection!");
            }
            else
            {
                context.Information("Found MSBuild Tool: {0}", msBuildToolPath);
                MSBuildToolPath = msBuildToolPath;
            }
        }
    }

    private static string GetVersionRange(MSBuildToolVersion toolVersion, bool minimumRange)
    {
        string minimumVersion = string.Empty;
        string maximumVersion = string.Empty;

        switch (toolVersion)
        {
            case MSBuildToolVersion.VS2005:
                minimumVersion = "8.0";
                maximumVersion = "9.0";
                break;

            case MSBuildToolVersion.VS2008:
                minimumVersion = "9.0";
                maximumVersion = "10.0";
                break;

            case MSBuildToolVersion.VS2010:
                minimumVersion = "10.0";
                maximumVersion = "12.0";
                break;

            case MSBuildToolVersion.VS2013:
                minimumVersion = "12.0";
                maximumVersion = "14.0";
                break;

            case MSBuildToolVersion.VS2015:
                minimumVersion = "14.0";
                maximumVersion = "16.0";
                break;

            case MSBuildToolVersion.VS2017:
                minimumVersion = "15.0";
                maximumVersion = "16.0";
                break;

            case MSBuildToolVersion.VS2019:
                minimumVersion = "16.0";
                maximumVersion = "17.0";
                break;
        }

        if (string.IsNullOrEmpty(minimumVersion) && string.IsNullOrEmpty(maximumVersion))
        {
            return null;
        }
        else if (minimumRange)
        {
            return minimumVersion;
        }
        else
        {
            return string.Format("[{0},{1})", minimumVersion, maximumVersion);
        }
    }

    private static FilePath ResolveVisualStudioMsBuildPath(ICakeContext context, MSBuildToolVersion toolVersion, string versionRange = null)
    {
        var canUseMinimumFallback = false;

        if (string.IsNullOrEmpty(versionRange))
        {
            canUseMinimumFallback = true;
            versionRange = GetVersionRange(toolVersion, minimumRange: false);
        }

        var vsWhereProductSettings = new VSWhereProductSettings
        {
            Version = versionRange,
        };

        if (!string.IsNullOrEmpty(versionRange))
        {
            context.Information("Resolving Visual Studio products using version range '{0}'.", versionRange);
        }
        else
        {
            context.Information("Resolving Visual Studio products without using a version range.");
        }

        var msBuildPath = GetMsBuildToolPath(context, context.VSWhereProducts("*", vsWhereProductSettings));

        if (msBuildPath != null || (versionRange == null && toolVersion == MSBuildToolVersion.VS2019))
        {
            return msBuildPath;
        }

        var vsWhereLegacySettings = new VSWhereLegacySettings
        {
            Version = versionRange
        };

        if (!string.IsNullOrEmpty(versionRange))
        {
            context.Information("Resolving Legacy Visual Studio installation using version range '{0}'.", versionRange);
        }
        else
        {
            context.Information("Resolving Legacy Visual Studio installation without using a version range.");
        }

        msBuildPath = GetMsBuildToolPath(context, context.VSWhereLegacy(vsWhereLegacySettings));

        if (msBuildPath != null || versionRange == null)
        {
            return msBuildPath;
        }

        if (canUseMinimumFallback)
        {
            return ResolveVisualStudioMsBuildPath(context, toolVersion, GetVersionRange(toolVersion, minimumRange: true));
        }

        return null;
    }

    private static FilePath GetMsBuildToolPath(ICakeContext context, DirectoryPathCollection directories)
    {
        if (directories == null)
        {
            return null;
        }

        foreach (var installation in directories)
        {
            var path = context.GetFiles(installation + "/MSBuild/*/Bin/amd64/MSBuild.exe").FirstOrDefault();

            if (path != null)
            {
                return path;
            }
        }

        return null;
    }
}