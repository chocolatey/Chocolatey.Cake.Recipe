// Copyright © 2023 Chocolatey Software, Inc
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

BuildParameters.Tasks.DockerLogin = Task("DockerLogin")
    .WithCriteria(() => BuildParameters.DockerCredentials.HasCredentials, "Skipping because Docker Credentials were not provided.")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .Does(() => 
{
    DockerLogin(
        BuildParameters.DockerCredentials.User,
        BuildParameters.DockerCredentials.Password,
        BuildParameters.DockerCredentials.Server
    );
});

BuildParameters.Tasks.DockerBuild = Task("DockerBuild")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .Does(() =>
{
    var platform = BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows ? "windows" : "linux";

    var dockerBuildSettings = new DockerImageBuildSettings();
    dockerBuildSettings.Tag = new string[] {
        string.Format("{0}/{1}:v{2}-{3}", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.MajorMinorPatch, platform)
    };
    dockerBuildSettings.File = string.Format("docker/Dockerfile.{0}", platform);

    if (platform == "linux")
    {
        dockerBuildSettings.BuildArg = new string[] {
            "buildscript=build.official.sh"
        };
    }

    DockerBuild(
        dockerBuildSettings,
        BuildParameters.RootDirectoryPath.ToString()
    );
});

BuildParameters.Tasks.DockerPush = Task("DockerPush")
    .WithCriteria(() => BuildParameters.IsTagged && BuildParameters.Version.MajorMinorPatch == BuildParameters.Version.FullSemVersion, "Skipping because this isn't a tagged full release build.")
    .WithCriteria(() => BuildParameters.DockerCredentials.HasCredentials, "Skipping because Docker Credentials were not provided.")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .IsDependentOn("DockerLogin")
    .IsDependentOn("DockerBuild")
    .Does(() =>
{
    var platform = BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows ? "windows" : "linux";

    DockerPush(
        string.Format("{0}/{1}:v{2}-{3}", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.MajorMinorPatch, platform)
    );
});

BuildParameters.Tasks.DockerTagAsLatest = Task("DockerTagAsLatest")
    .WithCriteria(() => BuildParameters.BranchType == BranchType.Master && BuildParameters.IsTagged && BuildParameters.Version.MajorMinorPatch == BuildParameters.Version.FullSemVersion, "Skipping because this isn't a tagged full release build.")
    .WithCriteria(() => BuildParameters.DockerCredentials.HasCredentials, "Skipping because Docker Credentials were not provided.")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .IsDependentOn("DockerLogin")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("DockerPush")
    .Does(() =>
{
    var platform = BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows ? "windows" : "linux";
    var latestPlatformTag = string.Format("{0}/{1}:latest-{2}", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, platform);

    DockerTag(
        string.Format("{0}/{1}:v{2}-{3}", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.MajorMinorPatch, platform),
        latestPlatformTag
    );

    DockerPush(
        latestPlatformTag
    );
});

BuildParameters.Tasks.Docker = Task("Docker")
    .IsDependentOn("DockerLogin")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("DockerPush")
    .IsDependentOn("DockerTagAsLatest")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled");

BuildParameters.Tasks.DockerManifest = Task("DockerManifest")
    .WithCriteria(() => BuildParameters.IsTagged && BuildParameters.Version.MajorMinorPatch == BuildParameters.Version.FullSemVersion, "Skipping because this isn't a tagged full release build.")
    .WithCriteria(() => BuildParameters.DockerCredentials.HasCredentials, "Skipping because Docker Credentials were not provided.")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .IsDependentOn("DockerLogin")
    .Does(() =>
{
    // Note: This will fail if one of the expected tags are not available, so it's important to ensure other builds have completed.

    // Create the version manifest
    var manifestListName = string.Format("{0}/{1}:v{2}", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.MajorMinorPatch);

    DockerManifestCreate(
        manifestListName,
        string.Format("{0}-windows", manifestListName),
        new string[] {
            string.Format("{0}-linux", manifestListName)
        }
    );

    DockerManifestPush(manifestListName);

    // Create the latest manifest only if this is the master branch.
    if (BuildParameters.BranchType == BranchType.Master) {
        DockerManifestCreate(
            string.Format("{0}/{1}:latest", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName),
            string.Format("{0}/{1}:latest-windows", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName),
            new string[] {
                string.Format("{0}/{1}:latest-linux", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName)
            }
        );

        DockerManifestPush(string.Format("{0}/{1}:latest", BuildParameters.RepositoryOwner, BuildParameters.RepositoryName));
    }
});