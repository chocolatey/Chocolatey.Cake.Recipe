public static class ToolSettings
{
    static ToolSettings()
    {
        SetToolPreprocessorDirectives();
    }

    public static string TestCoverageFilter { get; private set; }
    public static string TestCoverageExcludeByAttribute { get; private set; }
    public static string TestCoverageExcludeByFile { get; private set; }
    public static PlatformTarget BuildPlatformTarget { get; private set; }
    public static MSBuildToolVersion BuildMSBuildToolVersion { get; private set; }
    public static int MaxCpuCount { get; private set; }
    public static DirectoryPath OutputDirectory { get; private set; }
    public static FilePath EazfuscatorToolLocation { get; private set; }

    public static string GitVersionTool { get; private set; }
    public static string GitReleaseManagerTool { get; private set; }
    public static string MSBuildExtensionPackTool { get; private set; }
    public static string NUnitTool { get; private set; }
    public static string OpenCoverTool { get; private set; }
    public static string ReportGeneratorTool { get; private set; }
    public static string ReportUnitTool { get; private set; }
    public static string ReSharperReportsTool { get; private set; }
    public static string ReSharperTools { get; private set; }
    public static string StrongNameSignerTool { get; private set; }
    public static string XUnitTool { get; private set; }
    public static string ILMergeTool { get; private set; }

    public static string ReportGeneratorGlobalTool { get; private set; }
    public static string GitReleaseManagerGlobalTool { get; private set; }

    public static void SetToolPreprocessorDirectives(
        // This is specifically pinned to 5.0.1 as later versions break compatibility with Unix.
        string gitVersionTool = "#tool nuget:?package=GitVersion.CommandLine&version=5.0.1",
        string gitReleaseManagerTool = "#tool nuget:?package=GitReleaseManager&version=0.11.0",
        string msbuildExtensionPackTool = "#tool nuget:?package=MSBuild.Extension.Pack&version=1.9.0",
        string nunitTool = "#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0",
        string openCoverTool = "#tool nuget:?package=OpenCover&version=4.7.922",
        string reportGeneratorTool = "#tool nuget:?package=ReportGenerator&version=3.1.2",
        string reportUnitTool = "#tool nuget:?package=ReportUnit&version=1.2.1",
        string reSharperReportsTool = "#tool nuget:?package=ReSharperReports&version=0.2.0",
        string reSharperTools = "#tool nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2017.2.0",
        string strongNameSignerTool = "#tool nuget:?package=Brutal.Dev.StrongNameSigner&version=2.6.0",
        string xunitTool = "#tool nuget:?package=xunit.runner.console&version=2.4.1",
        string ilMergeTool = "#tool nuget:?package=ilmerge&version=3.0.41",
        string reportGeneratorGlobalTool = "#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.8.5",
        string gitReleaseManagerGlobalTool = "#tool dotnet:?package=GitReleaseManager.Tool&version=0.11.0"
    )
    {
        GitVersionTool = gitVersionTool;
        GitReleaseManagerTool = gitReleaseManagerTool;
        ReSharperTools = reSharperTools;
        ReSharperReportsTool = reSharperReportsTool;
        MSBuildExtensionPackTool = msbuildExtensionPackTool;
        XUnitTool = xunitTool;
        NUnitTool = nunitTool;
        OpenCoverTool = openCoverTool;
        ReportGeneratorTool = reportGeneratorTool;
        ReportUnitTool = reportUnitTool;
        StrongNameSignerTool = strongNameSignerTool;
        ILMergeTool = ilMergeTool;
        ReportGeneratorGlobalTool = reportGeneratorGlobalTool;
        GitReleaseManagerGlobalTool = gitReleaseManagerGlobalTool;
    }

    public static void SetToolSettings(
        ICakeContext context,
        string testCoverageFilter = null,
        string testCoverageExcludeByAttribute = null,
        string testCoverageExcludeByFile = null,
        PlatformTarget? buildPlatformTarget = null,
        MSBuildToolVersion buildMSBuildToolVersion = MSBuildToolVersion.Default,
        int? maxCpuCount = null,
        DirectoryPath outputDirectory = null,
        FilePath eazfuscatorToolLocation = null
    )
    {
        context.Information("Setting up tools...");

        var absoluteTestDirectory = context.MakeAbsolute(BuildParameters.TestDirectoryPath);
        var absoluteSourceDirectory = context.MakeAbsolute(BuildParameters.SolutionDirectoryPath);
        TestCoverageFilter = testCoverageFilter ?? string.Format("+[{0}*]* -[*.Tests]*", BuildParameters.Title);
        TestCoverageExcludeByAttribute = testCoverageExcludeByAttribute ?? "*.ExcludeFromCodeCoverage*";
        TestCoverageExcludeByFile = testCoverageExcludeByFile ?? "*/*Designer.cs;*/*.g.cs;*/*.g.i.cs";
        BuildPlatformTarget = buildPlatformTarget ?? PlatformTarget.MSIL;
        BuildMSBuildToolVersion = buildMSBuildToolVersion;
        MaxCpuCount = maxCpuCount ?? 0;
        OutputDirectory = outputDirectory;
        EazfuscatorToolLocation = eazfuscatorToolLocation ?? "./lib/Eazfuscator.NET/Eazfuscator.NET.exe";
    }
}