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

BuildParameters.Tasks.SubmitToVirusTotalTask = Task("Submit-To-VirusTotal")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldSubmitToVirusTotal, "Skipping since submission to VirusTotal has been disabled")
    .WithCriteria(() => BuildParameters.IsTagged || string.Equals(BuildParameters.Target, "Submit-To-VirusTotal", StringComparison.OrdinalIgnoreCase), "Skipping because current commit is not tagged, and target name is not Submit-To-VirusTotal")
    .Does(() =>
{
    if (BuildParameters.GetFilesToSubmitToVirusTotal == null)
    {
        Information("There are no files defined to be submitted to VirusTotal.");
        return;
    }

    var filesToSubmit = BuildParameters.GetFilesToSubmitToVirusTotal();

    if (filesToSubmit == null || !filesToSubmit.Any())
    {
        Information("There are no files defined to be submitted to VirusTotal.");
        return;
    }

    var virusTotalCredentials = VirusTotalCredentials.FetchCredentials(Context);

    if (string.IsNullOrWhiteSpace(virusTotalCredentials.ApiKey))
    {
        Warning("Unable to submit files to VirusTotal as no API Key has been provided. This can be set using the {0} environment variable.", Environment.VirusTotalApiKeyVariable);
        return;
    }

    // Only install the vt-cli tool once there are actually files to submit and an API Key to
    // submit them with
    RequireTool(ToolSettings.VirusTotalTool, () =>
    {
        // There isn't a Cake Addin for vt-cli, so resolve the shimmed
        // executable and drive it using the built in process aliases.
        var vtToolLocation = Context.Tools.Resolve("vt.exe");

        if (vtToolLocation == null)
        {
            Warning("Couldn't resolve the vt.exe tool, so unable to submit files to VirusTotal.");
            return;
        }

        Information("Using vt-cli from: {0}", vtToolLocation);

        // Guard against an incorrect API Key by making a lightweight, authenticated call
        // (which uploads nothing) before submitting any files. Without this, an invalid key
        // would upload every file, only to have each individual submission rejected.
        Information("Verifying the VirusTotal API Key...");

        IEnumerable<string> apiKeyCheckOutput;
        IEnumerable<string> apiKeyCheckError;
        var apiKeyCheckExitCode = StartProcess(vtToolLocation, new ProcessSettings {
            Arguments = new ProcessArgumentBuilder().Append("meta"),
            EnvironmentVariables = new Dictionary<string, string>
            {
                { "VTCLI_APIKEY", virusTotalCredentials.ApiKey }
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, out apiKeyCheckOutput, out apiKeyCheckError);

        if (apiKeyCheckExitCode != 0)
        {
            var apiKeyCheckErrorText = string.Join(System.Environment.NewLine, apiKeyCheckError ?? Enumerable.Empty<string>());

            if (apiKeyCheckErrorText.IndexOf("Wrong API key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new Exception(string.Format("Unable to submit files to VirusTotal because the provided API Key was rejected. Verify the value of the {0} environment variable.", Environment.VirusTotalApiKeyVariable));
            }

            throw new Exception(string.Format("Unable to submit files to VirusTotal because VirusTotal could not be reached (vt exit code {0}). {1}", apiKeyCheckExitCode, apiKeyCheckErrorText).Trim());
        }

        var failures = new List<string>();

        foreach (var fileToSubmit in filesToSubmit)
        {
            if (!FileExists(fileToSubmit))
            {
                Warning("The file expected ({0}) was not found for submission to VirusTotal.", fileToSubmit);
                continue;
            }

            Information("Submitting '{0}' to VirusTotal...", fileToSubmit);

            var exitCode = StartProcess(vtToolLocation, new ProcessSettings {
                Arguments = new ProcessArgumentBuilder()
                    .Append("scan")
                    .Append("file")
                    .AppendQuoted(MakeAbsolute(fileToSubmit).FullPath),
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "VTCLI_APIKEY", virusTotalCredentials.ApiKey }
                }
            });

            if (exitCode != 0)
            {
                Warning("Submission of '{0}' to VirusTotal returned a non-zero exit code ({1}).", fileToSubmit, exitCode);
                failures.Add(fileToSubmit.GetFilename().ToString());
            }
        }

        if (failures.Count != 0)
        {
            throw new Exception(string.Format("Failed to submit the following files to VirusTotal: {0}", string.Join(", ", failures)));
        }
    });
})
.OnError(exception =>
{
    Error(exception.Message);
    Information("Submit-To-VirusTotal Task failed, but continuing with next Task...");
    publishingError = true;
});
