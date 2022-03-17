public class BuildTasks
{

    // Configuration Builder Tasks
    public CakeTaskBuilder ConfigurationBuilderTask { get; set; }

    // Build Provider Tasks
    public CakeTaskBuilder PrintCiProviderEnvironmentVariablesTask { get; set; }

    // Build Tasks
    public CakeTaskBuilder CleanTask { get; set; }
    public CakeTaskBuilder RestoreTask { get; set; }
    public CakeTaskBuilder DotNetCoreRestoreTask { get; set; }
    public CakeTaskBuilder BuildTask { get; set; }
    public CakeTaskBuilder DotNetCoreBuildTask { get; set; }
    public CakeTaskBuilder PackageTask { get; set; }
    public CakeTaskBuilder DefaultTask { get; set; }
    public CakeTaskBuilder UploadArtifactsTask { get; set; }
    public CakeTaskBuilder ContinuousIntegrationTask { get; set; }
    public CakeTaskBuilder BuildMsiTask { get; set; }

    // Analysing Tasks
    public CakeTaskBuilder InspectCodeTask { get; set; }
    public CakeTaskBuilder CreateIssuesReportTask { get; set; }
    public CakeTaskBuilder AnalyzeTask { get; set; }

    // Eazfuscator Tasks
    public CakeTaskBuilder ObfuscateAssembliesTask { get; set; }

    // Packaging Tasks
    public CakeTaskBuilder CopyNuspecFolderTask { get; set; }
    public CakeTaskBuilder CreateChocolateyPackagesTask { get; set; }
    public CakeTaskBuilder CreateNuGetPackagesTask { get; set; }
    public CakeTaskBuilder DotNetCorePackTask { get; set; }
    public CakeTaskBuilder PublishPreReleasePackagesTask { get; set; }
    public CakeTaskBuilder PublishReleasePackagesTask { get; set; }

    // Testing Tasks
    public CakeTaskBuilder InstallOpenCoverTask { get; set; }
    public CakeTaskBuilder TestNUnitTask { get; set; }
    public CakeTaskBuilder TestxUnitTask { get; set; }
    public CakeTaskBuilder DotNetCoreTestTask { get; set; }
    public CakeTaskBuilder GenerateFriendlyTestReportTask { get; set; }
    public CakeTaskBuilder ReportCodeCoverageMetricsTask { get; set; }
    public CakeTaskBuilder GenerateLocalCoverageReportTask { get; set; }
    public CakeTaskBuilder TestTask { get; set; }

    // Strong Name Tasks
    public CakeTaskBuilder StrongNameSignerTask { get; set; }
    public CakeTaskBuilder InstallSNRemoveTask { get; set; }
    public CakeTaskBuilder ChangeStrongNameSignaturesTask { get; set; }

    // Signing Tasks
    public CakeTaskBuilder SignPowerShellScriptsTask { get; set; }
    public CakeTaskBuilder SignAssembliesTask { get; set; }
    public CakeTaskBuilder SignMsisTask { get; set; }

    // GitReleaseManager Tasks
    public CakeTaskBuilder ReleaseNotesTask { get; set; }
    public CakeTaskBuilder CreateReleaseNotesTask { get; set; }
    public CakeTaskBuilder ExportReleaseNotesTask { get; set; }
    public CakeTaskBuilder PublishGitHubReleaseTask { get; set; }
    public CakeTaskBuilder LabelsTask { get; set; }
    public CakeTaskBuilder CreateDefaultLabelsTask { get; set; }

    // Transifex Tasks
    public CakeTaskBuilder TransifexPullTranslationsTask { get; set; }
    public CakeTaskBuilder TransifexPushSourceResourceTask { get; set; }
    public CakeTaskBuilder TransifexPushTranslationsTask { get; set; }
    public CakeTaskBuilder TransifexSetupTask { get; set; }
}
