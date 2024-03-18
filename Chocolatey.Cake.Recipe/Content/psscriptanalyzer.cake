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

BuildParameters.Tasks.PSScriptAnalyzerTask = Task("Run-PSScriptAnalyzer")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunPSScriptAnalyzer, "Skipping because PSScriptAnalyzer is not enabled")
    .WithCriteria(() => BuildParameters.ShouldRunAnalyze, "Skipping because running analysis tasks is not enabled")
    .Does(() => RequirePSModule("PSScriptAnalyzer", "1.21.0", () =>
        RequirePSModule("ConvertToSARIF", "1.0.0", () => {
            var powerShellAnalysisScript = GetFiles("./tools/Chocolatey.Cake.Recipe*/Content/run-psscriptanalyzer.ps1").FirstOrDefault();

            if (powerShellAnalysisScript == null)
            {
                Warning("Unable to find PowerShell Analysis script, so unable to run analysis.");
                return;
            }

            var outputFolder = MakeAbsolute(BuildParameters.Paths.Directories.PSScriptAnalyzerResults).FullPath;
            EnsureDirectoryExists(outputFolder);

            if (BuildParameters.GetPSScriptAnalyzerSettings != null)
            {
                foreach (var PSScriptAnalyzerSetting in BuildParameters.GetPSScriptAnalyzerSettings())
                {
                    Information(string.Format("Running PSScriptAnalyzer {0}", PSScriptAnalyzerSetting.Name));

                    StartPowershellFile(MakeAbsolute(powerShellAnalysisScript), new PowershellSettings()
                        .WithModule("PSScriptAnalyzer")
                        .WithModule("ConvertToSARIF")
                        .WithModule("Microsoft.PowerShell.Management")
                        .WithModule("Microsoft.PowerShell.Utility")
                        .OutputToAppConsole(false)
                        .WithArguments(args => {
                            args.AppendQuoted("AnalyzePath", PSScriptAnalyzerSetting.AnalysisPath.ToString())
                                .AppendQuoted("SettingsPath", PSScriptAnalyzerSetting.SettingsPath.ToString())
                                .AppendQuoted("OutputPath", outputFolder)
                                .AppendArray("ExcludePaths", PSScriptAnalyzerSetting.ExcludePaths);
                        }));
                }
            }
            else
            {
                Information("There are no PSScriptAnalyzer Settings defined for this build, running with default format checking settings.");

                var settingsFile = GetFiles("./tools/Chocolatey.Cake.Recipe*/Content/formatting-settings.psd1").FirstOrDefault();

                if (settingsFile == null)
                {
                    Warning("Unable to find PowerShell Analysis settings, so unable to run analysis.");
                    return;
                }

                var pwshSettings = new PowershellSettings()
                    .WithModule("PSScriptAnalyzer")
                    .WithModule("ConvertToSARIF")
                    .WithModule("Microsoft.PowerShell.Management")
                    .WithModule("Microsoft.PowerShell.Utility")
                    .OutputToAppConsole(false)
                    .WithArguments(args => {
                        args.AppendQuoted("AnalyzePath", BuildParameters.RootDirectoryPath.ToString())
                            .AppendQuoted("SettingsPath", settingsFile.ToString())
                            .AppendQuoted("OutputPath", outputFolder)
                            .AppendArray("ExcludePaths", ToolSettings.PSScriptAnalyzerExcludePaths);
                    });

                pwshSettings.ExceptionOnScriptError = false;

                var resultCollection = StartPowershellFile(MakeAbsolute(powerShellAnalysisScript), pwshSettings);
                var returnCode = int.Parse(resultCollection[0].BaseObject.ToString());

                Information("Result: {0}", returnCode);

                // NOTE: Ideally, we would have this throw an exception, however, during testing, invoking PSScriptAnalyzer
                // sometimes caused random "The term 'Get-Command' is not recognized as the name of a cmdlet, function, script
                // file, or operable program." errors, which meant that we can't rely on this returnCode.  For now, we
                // only want PSScriptAnalyzer to warn on violations, so we don't need to worry about this just now.
                //if (returnCode != 0)
                //{
                //    throw new ApplicationException("Script failed to execute");
                //}
            }
        })
    )
);

public class PSScriptAnalyzerSettings
{
    public FilePath AnalysisPath { get; set; }
    public FilePath SettingsPath { get; set; }
    public List<String> ExcludePaths { get; set; }
    public string Name { get; set; }

    public PSScriptAnalyzerSettings()
    {
        Name = "Unnamed";
    }

    public PSScriptAnalyzerSettings(FilePath analysisPath,
                                    FilePath settingsPath,
                                    List<String> excludePaths = null,
                                    string name = "Unnamed")
    {
        AnalysisPath = analysisPath;
        SettingsPath = settingsPath;
        ExcludePaths = excludePaths;
        Name = name;
    }
}