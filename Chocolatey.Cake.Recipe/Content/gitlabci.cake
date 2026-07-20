// Copyright © 2026 Chocolatey Software, Inc
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
// BUILD PROVIDER
///////////////////////////////////////////////////////////////////////////////

public class GitLabCITagInfo : ITagInfo
{
    public GitLabCITagInfo(ICakeContext context)
    {
        // GitLab sets CI_COMMIT_TAG only when the pipeline is running for a tag.
        var tempName = context.EnvironmentVariable("CI_COMMIT_TAG");
        if (!string.IsNullOrEmpty(tempName))
        {
            IsTag = true;
            Name = tempName;
        }
    }

    public bool IsTag { get; }

    public string Name { get; }
}

public class GitLabCIRepositoryInfo : IRepositoryInfo
{
    public GitLabCIRepositoryInfo(ICakeContext context)
    {
        // CI_PROJECT_PATH is "namespace/project", matching the "owner/repo" style
        // that the GitHub Actions provider reports.
        Name = context.EnvironmentVariable("CI_PROJECT_PATH");

        var targetBranch = context.EnvironmentVariable("CI_MERGE_REQUEST_TARGET_BRANCH_NAME");
        if (!string.IsNullOrEmpty(targetBranch))
        {
            // Merge request pipeline: classify against the target branch, in the
            // same way the GitHub provider uses GITHUB_BASE_REF for pull requests.
            Branch = targetBranch;
        }
        else
        {
            // Branch pipelines expose CI_COMMIT_BRANCH; tag pipelines do not (HEAD
            // is detached on the tag), so fall back to deriving the branch that
            // contains the tag - mirroring the GitHub Actions provider.
            var branch = context.EnvironmentVariable("CI_COMMIT_BRANCH");
            if (string.IsNullOrEmpty(branch))
            {
                var tag = context.EnvironmentVariable("CI_COMMIT_TAG");
                if (!string.IsNullOrEmpty(tag))
                {
                    branch = GetBranchContainingTag(context, tag);
                }
                else
                {
                    // Last resort (unusual pipeline sources): CI_COMMIT_REF_NAME is
                    // always set to the branch or tag name the pipeline ran for.
                    branch = context.EnvironmentVariable("CI_COMMIT_REF_NAME");
                }
            }

            Branch = branch;
        }

        Tag = new GitLabCITagInfo(context);
    }

    private static string GetBranchContainingTag(ICakeContext context, string tag)
    {
        // Requires remote-tracking branches (refs/remotes/origin/*) to be present.
        // GitLab's default tag-pipeline clone does NOT create them, so the CI job
        // must fetch them first, e.g.:
        //   git fetch origin "+refs/heads/*:refs/remotes/origin/*"
        // Without that, `git branch -r --contains` finds nothing and we fall back
        // to the tag name below.
        var gitTool = context.Tools.Resolve("git");
        if (gitTool == null)
        {
            gitTool = context.Tools.Resolve("git.exe");
        }

        if (gitTool == null)
        {
            return tag;
        }

        IEnumerable<string> redirectedStandardOutput;
        IEnumerable<string> redirectedError;

        var exitCode = context.StartProcess(
            gitTool,
            new ProcessSettings {
                Arguments = "branch -r --contains " + tag,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            out redirectedStandardOutput,
            out redirectedError
        );

        if (exitCode == 0)
        {
            var lines = redirectedStandardOutput.ToList();
            if (lines.Count != 0)
            {
                return lines[0].TrimStart(new []{ ' ', '*' }).Replace("origin/", string.Empty);
            }
        }

        return tag;
    }

    public string Branch { get; }

    public string Name { get; }

    public ITagInfo Tag { get; }
}

public class GitLabCIPullRequestInfo : IPullRequestInfo
{
    public GitLabCIPullRequestInfo(ICakeContext context)
    {
        // A merge request pipeline sets CI_MERGE_REQUEST_IID (and reports a
        // pipeline source of "merge_request_event").
        var mergeRequestIid = context.EnvironmentVariable("CI_MERGE_REQUEST_IID");
        var pipelineSource = context.EnvironmentVariable("CI_PIPELINE_SOURCE");

        IsPullRequest = !string.IsNullOrEmpty(mergeRequestIid)
            || StringComparer.OrdinalIgnoreCase.Equals(pipelineSource, "merge_request_event");
    }

    public bool IsPullRequest { get; }
}

public class GitLabCIBuildInfo : IBuildInfo
{
    public GitLabCIBuildInfo(ICakeContext context)
    {
        // CI_PIPELINE_IID is the project-scoped, incrementing pipeline number -
        // the closest analogue to GitHub's workflow run number.
        Number = context.EnvironmentVariable("CI_PIPELINE_IID");
    }

    public string Number { get; }
}

public class GitLabCIBuildProvider : IBuildProvider
{
    private readonly ICakeContext _context;

    public GitLabCIBuildProvider(ICakeContext context)
    {
        Build = new GitLabCIBuildInfo(context);
        PullRequest = new GitLabCIPullRequestInfo(context);
        Repository = new GitLabCIRepositoryInfo(context);

        _context = context;
    }

    public IBuildInfo Build { get; }

    public IPullRequestInfo PullRequest { get; }

    public IRepositoryInfo Repository { get; }

    public bool SupportsTokenlessCodecov { get; } = false;

    public BuildProviderType Type { get; } = BuildProviderType.GitLabCI;

    public IEnumerable<string> PrintVariables { get; } = new[] {
        "CI",
        "CI_SERVER",
        "GITLAB_CI",
        "CI_PIPELINE_SOURCE",
        "CI_PIPELINE_ID",
        "CI_PIPELINE_IID",
        "CI_PROJECT_ID",
        "CI_PROJECT_NAME",
        "CI_PROJECT_PATH",
        "CI_PROJECT_NAMESPACE",
        "CI_PROJECT_URL",
        "CI_COMMIT_SHA",
        "CI_COMMIT_REF_NAME",
        "CI_COMMIT_BRANCH",
        "CI_COMMIT_TAG",
        "CI_MERGE_REQUEST_IID",
        "CI_MERGE_REQUEST_SOURCE_BRANCH_NAME",
        "CI_MERGE_REQUEST_TARGET_BRANCH_NAME",
        "CI_JOB_ID",
        "CI_JOB_NAME",
        "CI_RUNNER_ID",
        "CI_SERVER_HOST"
    };

    public void UploadArtifact(FilePath file)
    {
        _context.Information("Uploading artifact from path: {0}", file.FullPath);
        _context.Information("Uploading artifacts is currently not supported in Chocolatey.Cake.Recipe. Please use the artifacts keyword in your .gitlab-ci.yml");
    }
}
