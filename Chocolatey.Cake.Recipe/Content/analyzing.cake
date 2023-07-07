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

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////
using System.Diagnostics;

public void LaunchDefaultProgram(FilePath file) {
    FilePath program;
    string arguments = "";

    if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows)
    {
        program = "cmd";
        arguments = "/c start ";
    }
    else if ((program = Context.Tools.Resolve("xdg-open")) == null &&
             (program = Context.Tools.Resolve("open")) == null)
    {
        Warning("Unable to open report file: {0}", file.ToString());
        return;
    }

    arguments += " " + file.FullPath;
    // We can't use the StartProcess alias as this won't actually open the file.
    Process.Start(new ProcessStartInfo(program.FullPath, arguments) { CreateNoWindow = true });
}

BuildParameters.Tasks.InspectCodeTask = Task("InspectCode")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunInspectCode, "Skipping because InspectCode has been disabled")
    .WithCriteria(() => BuildParameters.ShouldRunAnalyze, "Skipping because running analysis tasks is not enabled")
    .Does<BuildData>(data => RequireTool(ToolSettings.ReSharperTools, () => {
        var inspectCodeLogFilePath = BuildParameters.Paths.Directories.InspectCodeTestResults.CombineWithFilePath("inspectcode.xml");

        var settings = new InspectCodeSettings() {
            SolutionWideAnalysis = true,
            OutputFile = inspectCodeLogFilePath
        };

        if (FileExists(BuildParameters.SourceDirectoryPath.CombineWithFilePath(BuildParameters.ResharperSettingsFileName)))
        {
            settings.Profile = BuildParameters.SourceDirectoryPath.CombineWithFilePath(BuildParameters.ResharperSettingsFileName);
        }

        InspectCode(BuildParameters.SolutionFilePath, settings);

        // Parse issues.
        var issues =
            ReadIssues(
                InspectCodeIssuesFromFilePath(inspectCodeLogFilePath),
                data.RepositoryRoot);
        Information("{0} InspectCode issues are found.", issues.Count());
        data.AddIssues(issues);

        if (FileExists(inspectCodeLogFilePath))
        {
            BuildParameters.BuildProvider.UploadArtifact(inspectCodeLogFilePath);
        }
    })
);

BuildParameters.Tasks.CreateIssuesReportTask = Task("CreateIssuesReport")
    .IsDependentOn("InspectCode")
    .WithCriteria(() => BuildParameters.ShouldRunAnalyze, "Skipping because running analysis tasks is not enabled")
    .Does<BuildData>(data => {
        var issueReportFile = BuildParameters.Paths.Directories.TestResults.CombineWithFilePath("issues-report.html");

        CreateIssueReport(
            data.Issues,
            GenericIssueReportFormatFromEmbeddedTemplate(GenericIssueReportTemplate.HtmlDxDataGrid),
            "./",
            issueReportFile);

        if (FileExists(issueReportFile))
        {
            BuildParameters.BuildProvider.UploadArtifact(issueReportFile);
        }
    });

BuildParameters.Tasks.AnalyzeTask = Task("Analyze")
    .IsDependentOn("InspectCode")
    .IsDependentOn("CreateIssuesReport")
    .WithCriteria(() => BuildParameters.ShouldRunAnalyze, "Skipping because running analysis tasks is not enabled");
