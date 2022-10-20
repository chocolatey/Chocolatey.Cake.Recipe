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

public static bool TransifexUserSettingsExists(ICakeContext context)
{
    var path = GetTransifexUserSettingsPath();
    return context.FileExists(path);
}

public static string GetTransifexUserSettingsPath()
{
    var path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/.transifexrc");
    return path;
}

public static bool TransifexIsConfiguredForRepository(ICakeContext context)
{
    return context.FileExists("./.tx/config");
}

// Before we do anything with transifex, we must make sure that it has been properly
// Initialized, this is mostly related to running on appveyor or other CI.
// Because we expect the repository to already be configured to use
// transifex, we cannot run tx init, or it would replace the repository configuration file.
BuildParameters.Tasks.TransifexSetupTask = Task("Transifex-Setup")
    .WithCriteria(() => BuildParameters.ShouldRunTransifex, "Skipping because Transifex is not enabled")
    .WithCriteria(() => !TransifexUserSettingsExists(Context), "Skipping because Transifex user settings already exist")
    .WithCriteria(() => BuildParameters.Transifex.HasCredentials, "Skipping because the Transifex credentials are missing")
    .Does(() =>
    {
        var path = GetTransifexUserSettingsPath();
        var encoding = new System.Text.UTF8Encoding(false);
        var text = string.Format("[https://www.transifex.com]\r\nhostname = https://www.transifex.com\r\npassword = {0}\r\nusername = api", BuildParameters.Transifex.ApiToken);
        System.IO.File.WriteAllText(path, text, encoding);
    });

BuildParameters.Tasks.TransifexPushSourceResourceTask = Task("Transifex-Push-SourceFiles")
    .WithCriteria(() => BuildParameters.ShouldRunTransifex, "Skipping because Transifex is not enabled")
    .WithCriteria(() => BuildParameters.Transifex.HasCredentials, "Skipping because the Transifex credentials are missing")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || string.Equals(BuildParameters.Target, "Transifex-Push-Translations", StringComparison.OrdinalIgnoreCase), "Skipping because this is a local build, and target name is not Transifex-Push-Translations")
    .IsDependentOn("Transifex-Setup")
    .Does(() =>
    {
        TransifexPush(new TransifexPushSettings {
            UploadSourceFiles = true,
            Force = string.Equals(BuildParameters.Target, "Transifex-Push-SourceFiles", StringComparison.OrdinalIgnoreCase)
        });
    });

BuildParameters.Tasks.TransifexPullTranslationsTask = Task("Transifex-Pull-Translations")
        .WithCriteria(() => BuildParameters.ShouldRunTransifex, "Skipping because Transifex is not enabled")
    .WithCriteria(() => BuildParameters.Transifex.HasCredentials, "Skipping because the Transifex credentials are missing")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => !BuildParameters.IsLocalBuild || string.Equals(BuildParameters.Target, "Transifex-Pull-Translations", StringComparison.OrdinalIgnoreCase), "Skipping because this is a local build, and target name is not Transifex-Pull-Translations")
    .IsDependentOn("Transifex-Push-SourceFiles")
    .Does(() =>
    {
        TransifexPull(new TransifexPullSettings {
            All = true,
            Mode = BuildParameters.TransifexPullMode,
            MinimumPercentage = BuildParameters.TransifexPullPercentage
        });
    });

BuildParameters.Tasks.TransifexPushTranslationsTask = Task("Transifex-Push-Translations")
    .Does(() =>
    {
        TransifexPush(new TransifexPushSettings {
            UploadTranslations = true
        });
    });
