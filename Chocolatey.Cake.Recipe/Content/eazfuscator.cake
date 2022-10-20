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

BuildParameters.Tasks.ObfuscateAssembliesTask = Task("Obfuscate-Assemblies")
    .WithCriteria(() => BuildParameters.ShouldObfuscateOutputAssemblies, "Skipping since obfuscating output assemblies has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetFilesToObfuscate != null)
    {
        var settings = new EazfuscatorNetSettings();

        if (BuildParameters.ShouldStrongNameOutputAssemblies)
        {
            settings.KeyFile = BuildParameters.StrongNameKeyPath;
        }

        var eazfuscatorToolLocation = Context.Tools.Resolve("Eazfuscator.NET.exe");

        if (eazfuscatorToolLocation == null)
        {
            Warning("Couldn't resolve EazFuscator.NET.Exe tool, so using value from ToolSettings: {0}", ToolSettings.EazfuscatorToolLocation);
            Context.Tools.RegisterFile(ToolSettings.EazfuscatorToolLocation);
        }
        else
        {
            Information("Using EazFuscator from: {0}", eazfuscatorToolLocation);
        }

        if (Context.Log.Verbosity == Verbosity.Verbose || Context.Log.Verbosity == Verbosity.Diagnostic)
        {
            settings.Statistics = true;
        }

        if (BuildParameters.IsDotNetBuild)
        {
            // Then run Eazfuscator once per file, since there is no ILMerge happening
            Information("Running EazFuscator once for each file...");

            foreach (var file in BuildParameters.GetFilesToObfuscate())
            {
                // This needs to be "reset" so that previously set properties aren't in place for the next iteration.
                settings = new EazfuscatorNetSettings();

                var fileName = file.GetFilenameWithoutExtension();
                var msbuildPathFilePath = new FilePath(string.Format("{0}/{1}/{1}.csproj", BuildParameters.SourceDirectoryPath.FullPath, fileName));

                if (FileExists(msbuildPathFilePath))
                {
                    settings.MSBuildProjectPath = msbuildPathFilePath;
                }

                EazfuscatorNet(file, settings);
            }
        }
        else
        {
            // Then run Eazfuscator once, for all files
            // This is due to the fact we are ILmerge'ing at teh same time, and all assets need
            // to be obfuscated, prior to teh merge happening.  We could try and do this ourselves
            // however, Eazfuscator already knows how to do it, so let it take care of it.
            Information("Running EazFuscator once for all files...");

            EazfuscatorNet(BuildParameters.GetFilesToObfuscate(), settings);
        }
    }
    else
    {
        Information("There are no files defined to be obfuscated.");
    }
});
