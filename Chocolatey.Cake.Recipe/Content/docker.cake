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

public bool ShouldPushToDockerInternalRegistry()
{
    // Every build of a long-lived branch is pushed, so that images can be tested before they are released.
    return BuildParameters.ShouldRunDocker &&
        !string.IsNullOrEmpty(BuildParameters.DockerInternalRegistry) &&
        (BuildParameters.IsTagged ||
            BuildParameters.BranchType == BranchType.Develop ||
            BuildParameters.BranchType == BranchType.Master ||
            BuildParameters.BranchType == BranchType.Release ||
            BuildParameters.BranchType == BranchType.HotFix ||
            BuildParameters.BranchType == BranchType.Support);
}

public string GetDockerImageName(string tagSuffix)
{
    // The Windows and Linux images are built by separate CI builds, each with their own build counter, so the
    // version can be passed in to make sure that both builds use the same tag.
    var imageVersion = string.IsNullOrEmpty(BuildParameters.DockerImageVersion)
        ? BuildParameters.Version.PackageVersion
        : BuildParameters.DockerImageVersion;

    // Without an internal registry, the image name has no registry prefix, and the image is only built locally.
    var registryPrefix = string.IsNullOrEmpty(BuildParameters.DockerInternalRegistry)
        ? string.Empty
        : string.Format("{0}/", BuildParameters.DockerInternalRegistry.TrimEnd('/'));

    // For example: registry.example.com:5000/chocolatey/choco:2.6.0-alpha-20260917-123-linux
    return string.Format("{0}{1}/{2}:{3}{4}", registryPrefix, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, imageVersion, tagSuffix);
}

BuildParameters.Tasks.DockerLogin = Task("DockerLogin")
    .WithCriteria(() => ShouldPushToDockerInternalRegistry(), "Skipping because this build isn't pushed to the internal Docker registry")
    .Does(() =>
{
    var dockerCredentials = DockerCredentials.FetchCredentials(Context);

    DockerLogin(
        dockerCredentials.User,
        dockerCredentials.Password,
        dockerCredentials.Server
    );
});

BuildParameters.Tasks.DockerBuild = Task("DockerBuild")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled")
    .Does(() =>
{
    var platform = BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows ? "windows" : "linux";

    var dockerBuildSettings = new DockerImageBuildSettings();
    dockerBuildSettings.Tag = new string[] {
        GetDockerImageName(string.Format("-{0}", platform))
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
    .WithCriteria(() => ShouldPushToDockerInternalRegistry(), "Skipping because this build isn't pushed to the internal Docker registry")
    .IsDependentOn("DockerLogin")
    .IsDependentOn("DockerBuild")
    .Does(() =>
{
    var platform = BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows ? "windows" : "linux";

    DockerPush(
        GetDockerImageName(string.Format("-{0}", platform))
    );
});

BuildParameters.Tasks.Docker = Task("Docker")
    .IsDependentOn("DockerLogin")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("DockerPush")
    .WithCriteria(() => BuildParameters.ShouldRunDocker, "Skipping because running Docker tasks is not enabled");

BuildParameters.Tasks.DockerManifest = Task("DockerManifest")
    .WithCriteria(() => ShouldPushToDockerInternalRegistry(), "Skipping because this build isn't pushed to the internal Docker registry")
    .IsDependentOn("DockerLogin")
    .Does(() =>
{
    // Note: This will fail if one of the expected tags are not available, so it's important to ensure other builds have completed.
    var manifestListName = GetDockerImageName(string.Empty);

    DockerManifestCreate(
        manifestListName,
        string.Format("{0}-windows", manifestListName),
        new string[] {
            string.Format("{0}-linux", manifestListName)
        }
    );

    DockerManifestPush(manifestListName);
});