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

BuildParameters.Tasks.DependencyCheckTask = Task("Dependency-Check")
    .WithCriteria(() => BuildParameters.ShouldRunDependencyCheck, "Skipping because DependencyCheck has been disabled")
    .WithCriteria(() => BuildParameters.ShouldRunSonarQube, "Skipping because running SonarQube has been disabled")
    .WithCriteria(() => !string.IsNullOrEmpty(BuildParameters.SonarQubeToken), "Skipping because SonarQube Token is undefined")
    .IsDependentOn("Initialize-SonarQube")
    .IsDependeeOf("Finalise-SonarQube")
    .Does(() => RequireTool(ToolSettings.DependencyCheckTool, () =>
{
    DownloadFile(
        "https://github.com/jeremylong/DependencyCheck/releases/download/v12.0.1/dependency-check-12.0.1-release.zip",
        BuildParameters.RootDirectoryPath.CombineWithFilePath("dependency-check.zip")
    );

    Unzip(
        BuildParameters.RootDirectoryPath.CombineWithFilePath("dependency-check.zip"),
        BuildParameters.RootDirectoryPath.FullPath
    );

    CopyDirectory(
        BuildParameters.RootDirectoryPath.Combine("dependency-check"),
        BuildParameters.RootDirectoryPath.Combine("tools/DependencyCheck.Runner.Tool.3.2.1/tools")
    );

    DeleteDirectory(
        BuildParameters.RootDirectoryPath.Combine("dependency-check"),
        new DeleteDirectorySettings {
            Recursive = true,
            Force     = true
        }
    );

    DeleteFile(BuildParameters.RootDirectoryPath.CombineWithFilePath("dependency-check.zip"));

    if (ToolSettings.DependencyCheckDisableYarnAudit)
    {
        ReplaceTextInFiles(
            BuildParameters.RootDirectoryPath.Combine("tools/DependencyCheck.Runner.Tool.3.2.1/tools/bin").CombineWithFilePath("dependency-check.bat").ToString(),
            "org.owasp.dependencycheck.App %CMD_LINE_ARGS%",
            "org.owasp.dependencycheck.App --disableYarnAudit %CMD_LINE_ARGS%"
        );
    };

    if (!string.IsNullOrEmpty(BuildParameters.DependencyCheckNvdApiKey))
    {
        ReplaceTextInFiles(
            BuildParameters.RootDirectoryPath.Combine("tools/DependencyCheck.Runner.Tool.3.2.1/tools/bin").CombineWithFilePath("dependency-check.bat").ToString(),
            "%CMD_LINE_ARGS%",
            string.Format("--nvdApiKey {0} %CMD_LINE_ARGS%", BuildParameters.DependencyCheckNvdApiKey)
        );
    };

    var DependencyCheckSettings = new DependencyCheckSettings {
        Project = BuildParameters.ProductName,
        Scan    = BuildParameters.SourceDirectoryPath.FullPath,
        Format  = "ALL",
        Out     = BuildParameters.Paths.Directories.DependencyCheckReports.FullPath
    };

    if (!string.IsNullOrEmpty(BuildParameters.DependencyCheckDb.ConnectionString) &&
        !string.IsNullOrEmpty(BuildParameters.DependencyCheckDb.UserName) &&
        !string.IsNullOrEmpty(BuildParameters.DependencyCheckDb.Password))
    {
        DependencyCheckSettings.ConnectionString   = BuildParameters.DependencyCheckDb.ConnectionString;
        DependencyCheckSettings.DatabaseUser       = BuildParameters.DependencyCheckDb.UserName;
        DependencyCheckSettings.DatabasePassword   = BuildParameters.DependencyCheckDb.Password;
        DependencyCheckSettings.DatabaseDriverName = BuildParameters.DependencyCheckDbDriverName;
        DependencyCheckSettings.DatabaseDriverPath = BuildParameters.Paths.Files.DependencyCheckDbDriverPath.ToString();
    };

    DependencyCheck(DependencyCheckSettings);
}));