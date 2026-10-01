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

BuildParameters.Tasks.PrintCiProviderEnvironmentVariablesTask = Task("Print-CI-Provider-Environment-Variables")
    .Does(() =>
{
        var variables = BuildParameters.BuildProvider.PrintVariables ?? Enumerable.Empty<string>();
        if (!variables.Any())
        {
            Information("No environment variables is available for current provider.");
            return;
        }

        var maxlen = variables.Max(v => v.Length);

        foreach (var variable in variables.OrderBy(v => v.Length).ThenBy(v => v))
        {
            var padKey = variable.PadLeft(maxlen);
            Information("{0}: {1}", padKey, EnvironmentVariable(variable));
        }
});

public interface ITagInfo
{
    bool IsTag { get; }

    string Name { get; }
}

public interface IRepositoryInfo
{
    string Branch { get; }

    string Name { get; }

    ITagInfo Tag { get; }
}

public interface IPullRequestInfo
{
    bool IsPullRequest { get; }
}

public interface IBuildInfo
{
    string Number { get; }
}

public interface IBuildProvider
{
    IRepositoryInfo Repository { get; }

    IPullRequestInfo PullRequest { get; }

    IBuildInfo Build { get; }

    bool SupportsTokenlessCodecov { get; }

    IEnumerable<string> PrintVariables { get; }

    void UploadArtifact(FilePath file);

    BuildProviderType Type { get; }
}

public enum BuildProviderType
{
    TeamCity,
    GitHubActions,
    GitLabCI,
    Local
}

// Resolves the branch a tag was made from, for build providers with no
// direct branch information for a tag ref (HEAD is detached on a tag
// checkout). Shared by the GitHub Actions, GitLab CI, and TeamCity
// providers, which otherwise each shelled out to `git branch -r --contains`
// with the same selection bug: a gitflow "finish" merges the release,
// master, hotfix, or support branch back into develop, so both branches can
// contain the tag, and picking the alphabetically-first candidate (what
// `git branch -r` returns) makes develop win over the branch that was
// actually tagged. See issue #316 for the ChocolateyGUI 3.3.0 failure this
// caused (Publish-Release-Notes skipped because BranchType resolved to
// Develop instead of Master).
// Requires remote-tracking branches (refs/remotes/origin/*) to be present.
// A default tag-pipeline clone does not always create them, so the CI job
// must fetch them first, e.g.:
//   git fetch origin "+refs/heads/*:refs/remotes/origin/*"
// Without that, `git branch -r --contains` finds nothing and this falls
// back to returning the tag name unchanged.
// masterBranchName is passed in explicitly (the same value BuildParameters
// will later expose as MasterBranchName) rather than read from
// BuildParameters directly - this runs from inside GetBuildProvider, which
// BuildParameters.SetParameters calls BEFORE it assigns MasterBranchName,
// so the static property is not yet populated at this point.
public static string GetBranchContainingTag(ICakeContext context, string tag, string masterBranchName)
{
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

    if (exitCode != 0)
    {
        return tag;
    }

    // A symbolic entry such as "origin/HEAD -> origin/master" can appear in
    // the output alongside the real branch names; strip it out rather than
    // risk it being picked and trimmed into a nonsense "branch" name.
    var candidates = redirectedStandardOutput
        .Select(line => line.TrimStart(' ', '*').Replace("origin/", string.Empty))
        .Where(name => !string.IsNullOrEmpty(name) && name.IndexOf("HEAD", StringComparison.Ordinal) < 0)
        .ToList();

    if (candidates.Count == 0)
    {
        return tag;
    }

    // Prefer a releasable branch over any other candidate, master first,
    // since master is the branch a tagged release most commonly targets.
    // Falling back to the first candidate (today's behaviour prior to this
    // fix) only when none of these patterns match preserves the correct
    // result for a tag made directly on develop (e.g. an alpha/prerelease
    // build, which has no release-type branch to prefer) and for a tag on
    // any other branch type.
    // Matching style (case-insensitive; release/hotfix/support matched as a
    // bare prefix, no trailing slash) mirrors BuildParameters.SetParameters'
    // own BranchType classification exactly, so a branch this picks is
    // classified the same way once BranchType is computed from it. Master
    // has to come from the masterBranchName parameter, since it is the one
    // of the four that is actually configurable (e.g. "main"); the other
    // three enum names are used directly, since there is no equivalent
    // configuration for them anywhere in the recipe - BranchType.ToString()
    // avoids re-hardcoding the same literal a second time, even though it
    // does not remove the duplication against SetParameters' own literals.
    var master = candidates.FirstOrDefault(name => StringComparer.OrdinalIgnoreCase.Equals(masterBranchName, name));
    if (master != null)
    {
        return master;
    }

    var release = candidates.FirstOrDefault(name => name.StartsWith(BranchType.Release.ToString(), StringComparison.OrdinalIgnoreCase));
    if (release != null)
    {
        return release;
    }

    var hotfix = candidates.FirstOrDefault(name => name.StartsWith(BranchType.HotFix.ToString(), StringComparison.OrdinalIgnoreCase));
    if (hotfix != null)
    {
        return hotfix;
    }

    var support = candidates.FirstOrDefault(name => name.StartsWith(BranchType.Support.ToString(), StringComparison.OrdinalIgnoreCase));
    if (support != null)
    {
        return support;
    }

    return candidates[0];
}

public static IBuildProvider GetBuildProvider(ICakeContext context, BuildSystem buildSystem, string masterBranchName)
{
    if (buildSystem.IsRunningOnTeamCity)
    {
        context.Information("Using TeamCity Provider...");
        return new TeamCityBuildProvider(buildSystem.TeamCity, context, masterBranchName);
    }

    if (buildSystem.IsRunningOnGitHubActions)
    {
        context.Information("Using GitHub Action Provider...");
        return new GitHubActionBuildProvider(context, masterBranchName);
    }

    if (buildSystem.IsRunningOnGitLabCI)
    {
        context.Information("Using GitLab CI Provider...");
        return new GitLabCIBuildProvider(context, masterBranchName);
    }

    // always fallback to Local Build
    context.Information("Using Local Build Provider...");
    return new LocalBuildBuildProvider(context);
}
