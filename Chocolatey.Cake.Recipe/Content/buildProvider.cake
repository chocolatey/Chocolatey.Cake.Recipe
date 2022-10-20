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
    Local
}

public static IBuildProvider GetBuildProvider(ICakeContext context, BuildSystem buildSystem)
{
    if (buildSystem.IsRunningOnTeamCity)
    {
        context.Information("Using TeamCity Provider...");
        return new TeamCityBuildProvider(buildSystem.TeamCity, context);
    }

    if (buildSystem.IsRunningOnGitHubActions)
    {
        context.Information("Using GitHub Action Provider...");
        return new GitHubActionBuildProvider(context);
    }

    // always fallback to Local Build
    context.Information("Using Local Build Provider...");
    return new LocalBuildBuildProvider(context);
}
