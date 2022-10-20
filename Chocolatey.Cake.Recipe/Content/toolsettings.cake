public static class ToolSettings
{
    static ToolSettings()
    {
        SetToolPreprocessorDirectives();
    }

    public static MSBuildToolVersion BuildMSBuildToolVersion { get; private set; }
    public static PlatformTarget BuildPlatformTarget { get; private set; }
    public static string XBuildPlatformTarget { get; private set; }
    public static FilePath EazfuscatorToolLocation { get; private set; }
    public static string AmazonLambdaGlobalTool { get; private set; }
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
    public static string StrongNameSignerTool { get; private set; }
    public static string TestCoverageExcludeByAttribute { get; private set; }
    public static string TestCoverageExcludeByFile { get; private set; }
    public static string TestCoverageFilter { get; private set; }
    public static string XUnitTool { get; private set; }

    public static void SetToolPreprocessorDirectives(
        string amazonLambdaGlobalTool = "#tool dotnet:?package=amazon.lambda.tools&version=5.4.5",
        string gitVersionGlobalTool = "#tool dotnet:?package=GitVersion.Tool&version=5.10.1",
        string gitVersionTool = "#tool nuget:?package=GitVersion.CommandLine&version=5.10.1",
        string gitReleaseManagerGlobalTool = "#tool dotnet:?package=GitReleaseManager.Tool&version=0.13.0",
        string gitReleaseManagerTool = "#tool nuget:?package=GitReleaseManager&version=0.13.0",
        string ilMergeTool = "#tool nuget:?package=ilmerge&version=3.0.41",
        string msbuildExtensionPackTool = "#tool nuget:?package=MSBuild.Extension.Pack&version=1.9.0",
        string nunitTool = "#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0",
        string openCoverTool = "#tool nuget:?package=OpenCover&version=4.7.1221",
        string reportGeneratorGlobalTool = "#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.8.5",
        string reportGeneratorTool = "#tool nuget:?package=ReportGenerator&version=5.1.6",
        string reportUnitTool = "#tool nuget:?package=ReportUnit&version=1.2.1",
        string reSharperReportsTool = "#tool nuget:?package=ReSharperReports&version=0.2.0",
        string reSharperTools = "#tool nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2017.2.0",
        string strongNameSignerTool = "#tool nuget:?package=Brutal.Dev.StrongNameSigner&version=2.6.0",
        string xunitTool = "#tool nuget:?package=xunit.runner.console&version=2.4.1"
    )
    {
        AmazonLambdaGlobalTool = amazonLambdaGlobalTool;
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
        StrongNameSignerTool = strongNameSignerTool;
        XUnitTool = xunitTool;
    }

    public static void SetToolSettings(
        ICakeContext context,
        PlatformTarget? buildPlatformTarget = null,
        string xBuildPlatformTarget = "Any CPU",
        MSBuildToolVersion buildMSBuildToolVersion = MSBuildToolVersion.Default,
        FilePath eazfuscatorToolLocation = null,
        int? maxCpuCount = null,
        DirectoryPath outputDirectory = null,
        string testCoverageExcludeByAttribute = null,
        string testCoverageExcludeByFile = null,
        string testCoverageFilter = null
    )
    {
        context.Information("Setting up tools...");

        BuildPlatformTarget = buildPlatformTarget ?? PlatformTarget.MSIL;
        XBuildPlatformTarget = xBuildPlatformTarget;
        BuildMSBuildToolVersion = buildMSBuildToolVersion;
        EazfuscatorToolLocation = eazfuscatorToolLocation ?? "./lib/Eazfuscator.NET/Eazfuscator.NET.exe";
        MaxCpuCount = maxCpuCount ?? 0;
        OutputDirectory = outputDirectory;
        TestCoverageExcludeByAttribute = testCoverageExcludeByAttribute ?? "*.ExcludeFromCodeCoverage*";
        TestCoverageExcludeByFile = testCoverageExcludeByFile ?? "*/*Designer.cs;*/*.g.cs;*/*.g.i.cs";
        TestCoverageFilter = testCoverageFilter ?? string.Format("+[{0}*]* +[{1}*]* -[*.tests]* -[*.Tests]*", BuildParameters.Title, BuildParameters.Title.ToLowerInvariant());
    }
}