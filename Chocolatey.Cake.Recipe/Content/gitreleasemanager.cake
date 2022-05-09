///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

BuildParameters.Tasks.CreateReleaseNotesTask = Task("Create-Release-Notes")
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if (BuildParameters.CanRunGitReleaseManager)
        {
            var settings = new GitReleaseManagerCreateSettings
            {
                Milestone         = BuildParameters.Version.Milestone,
                Name              = BuildParameters.Version.Milestone,
                TargetCommitish   = BuildParameters.MasterBranchName,
                Prerelease        = Context.HasArgument("create-pre-release")
            };
            if (settings.Prerelease)
            {
                settings.TargetCommitish = BuildParameters.BuildProvider.Repository.Branch;
            }

            GitReleaseManagerCreate(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, settings);
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
);

BuildParameters.Tasks.ExportReleaseNotesTask = Task("Export-Release-Notes")
    .WithCriteria(() => BuildParameters.ShouldDownloadMilestoneReleaseNotes || BuildParameters.ShouldDownloadFullReleaseNotes, "Skipping because exporting Release notes has been disabled")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || BuildParameters.PrepareLocalRelease, "Skipping because this is local build, and is not preparing local release")
    .WithCriteria(() => !BuildParameters.IsPullRequest || BuildParameters.PrepareLocalRelease, "Skipping because this is pull request, and is not preparing local release")
    .WithCriteria(() => BuildParameters.BranchType == BranchType.Master || BuildParameters.BranchType == BranchType.Release || BuildParameters.BranchType == BranchType.HotFix || BuildParameters.PrepareLocalRelease, "Skipping because this is not a releasable branch, and is not preparing local release")
    .WithCriteria(() => BuildParameters.IsTagged || BuildParameters.PrepareLocalRelease, "Skipping because this is not a tagged build, and is not preparing local release")
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if (BuildParameters.CanRunGitReleaseManager)
        {
            if (BuildParameters.ShouldDownloadMilestoneReleaseNotes)
            {
                var settings = new GitReleaseManagerExportSettings
                {
                    TagName = BuildParameters.Version.Milestone
                };

                GitReleaseManagerExport(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.MilestoneReleaseNotesFilePath, settings);
            }

            if (BuildParameters.ShouldDownloadFullReleaseNotes)
            {
                GitReleaseManagerExport(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.FullReleaseNotesFilePath);
            }
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
);

BuildParameters.Tasks.PublishGitHubReleaseTask = Task("Publish-GitHub-Release")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildParameters.IsLocalBuild, "Skipping because this is a local build")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because this is pull request")
    .WithCriteria(() => BuildParameters.BranchType == BranchType.Master || BuildParameters.BranchType == BranchType.Release || BuildParameters.BranchType == BranchType.HotFix, "Skipping because this is not a releasable branch")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because this is not a tagged build")
    .WithCriteria(() => BuildParameters.ShouldRunGitReleaseManager, "Skipping because this publishing running GitReleaseManager is disabled via parameters (perhaps the default value?")
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if (BuildParameters.CanRunGitReleaseManager)
        {
            // Concatenating FilePathCollections should make sure we get unique FilePaths
            foreach (var package in GetFiles(BuildParameters.Paths.Directories.Packages + "/*") +
                                   GetFiles(BuildParameters.Paths.Directories.NuGetPackages + "/*") +
                                   GetFiles(BuildParameters.Paths.Directories.ChocolateyPackages + "/*"))
            {
                GitReleaseManagerAddAssets(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone, package.ToString());
            }

            GitReleaseManagerClose(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone);
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
)
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
    publishingError = true;
});

BuildParameters.Tasks.CreateDefaultLabelsTask = Task("Create-Default-Labels")
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if (BuildParameters.CanRunGitReleaseManager)
        {
            GitReleaseManagerLabel(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName);
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
);
