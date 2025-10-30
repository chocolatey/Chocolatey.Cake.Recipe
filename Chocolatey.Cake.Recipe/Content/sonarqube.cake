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

BuildParameters.Tasks.InitializeSonarQubeTask = Task("Initialize-SonarQube")
    .WithCriteria(() => BuildParameters.ShouldRunSonarQube, "Skipping because SonarQube has been disabled")
    .Does(() => RequireTool(ToolSettings.SonarQubeTool, () =>
{
    var sonarQubeCredentials = SonarQubeCredentials.FetchCredentials(Context);

    var SonarQubeSettings = new SonarBeginSettings
    {
        Key     = BuildParameters.SonarQubeId,
        Version = BuildParameters.Version.InformationalVersion,
        Login   = sonarQubeCredentials.Token
    };

    if (!string.IsNullOrEmpty(BuildParameters.SonarQubeUrl))
    {
        SonarQubeSettings.Url = BuildParameters.SonarQubeUrl;
    };

    SonarBegin(SonarQubeSettings);
}));

BuildParameters.Tasks.FinaliseSonarQubeTask = Task("Finalise-SonarQube")
    .WithCriteria(() => BuildParameters.ShouldRunSonarQube, "Skipping because SonarQube has been disabled")
    .IsDependentOn("Initialize-SonarQube")
    .IsDependeeOf("Package")
    .Does(() => RequireTool(ToolSettings.SonarQubeTool, () =>
{
    var sonarQubeCredentials = SonarQubeCredentials.FetchCredentials(Context);

    SonarEnd(new SonarEndSettings {
        Login = sonarQubeCredentials.Token
    });
}));