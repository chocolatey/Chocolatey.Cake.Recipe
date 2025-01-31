// Copyright © 2025 Chocolatey Software, Inc
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
                TargetCommitish   = Context.Argument("target-branch", BuildParameters.MasterBranchName),
                Prerelease        = Context.HasArgument("create-pre-release")
            };

            if (!Context.HasArgument("target-branch") && (settings.Prerelease || BuildParameters.BranchType == BranchType.Support))
            {
                settings.TargetCommitish = BuildParameters.BuildProvider.Repository.Branch;
            }

            if (BuildParameters.RepositoryHostedInGitLab)
            {
                settings.ArgumentCustomization = args => args.Append("--provider GitLab");
            }

            GitReleaseManagerCreate(BuildParameters.GitReleaseManager.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, settings);
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available. Token not set in environment variable");
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
            var settings = new GitReleaseManagerExportSettings();

            if (BuildParameters.RepositoryHostedInGitLab)
            {
                settings.ArgumentCustomization = args => args.Append("--provider GitLab");
            }

            if (BuildParameters.ShouldDownloadMilestoneReleaseNotes)
            {
                settings.TagName = BuildParameters.Version.Milestone;

                GitReleaseManagerExport(BuildParameters.GitReleaseManager.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.MilestoneReleaseNotesFilePath, settings);
            }

            if (BuildParameters.ShouldDownloadFullReleaseNotes)
            {
                GitReleaseManagerExport(BuildParameters.GitReleaseManager.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.FullReleaseNotesFilePath, settings);
            }
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
);

BuildParameters.Tasks.PublishGitHubReleaseTask = Task("Publish-GitHub-Release")
    .WithCriteria(() => BuildParameters.BranchType == BranchType.Master || BuildParameters.BranchType == BranchType.Release || BuildParameters.BranchType == BranchType.HotFix, "Skipping because this is not a releasable branch")
    .WithCriteria(() => BuildParameters.IsTagged, "Skipping because this is not a tagged build")
    .Does(() => RequireTool(BuildParameters.IsDotNetBuild || BuildParameters.PreferDotNetGlobalToolUsage ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if (BuildParameters.CanRunGitReleaseManager)
        {
            // If we are running on GitLab, then we need to actually publish the Release,
            // since it will have been created with a date in the future. When running on
            // GitHub, this isn't required, since the Release is first created in Draft,
            // and it is published this the GitHub UI.
            if (BuildParameters.RepositoryHostedInGitLab)
            {
                var publishSettings = new GitReleaseManagerPublishSettings {
                    ArgumentCustomization = args => args.Append("--provider GitLab")
                };

                GitReleaseManagerPublish(BuildParameters.GitReleaseManager.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone, publishSettings);
            }

            // Next up, we close the milestone, which based on configuration, may add comments
            // to issues contained within the specified Milestone.
            var closeSettings = new GitReleaseManagerCloseMilestoneSettings();

            if (BuildParameters.RepositoryHostedInGitLab)
            {
                closeSettings.ArgumentCustomization = args => args.Append("--provider GitLab");
            }

            GitReleaseManagerClose(BuildParameters.GitReleaseManager.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone, closeSettings);
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
);