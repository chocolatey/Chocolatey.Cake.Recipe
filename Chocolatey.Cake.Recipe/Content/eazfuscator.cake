BuildParameters.Tasks.ObfuscateAssembliesTask = Task("Obfuscate-Assemblies")
    .WithCriteria(() => BuildParameters.ShouldObfuscateOutputAssemblies, "Skipping since obfuscating output assemblies has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetFilesToObfuscate != null)
    {
        foreach (var file in BuildParameters.GetFilesToObfuscate())
        {
            var fileName = file.GetFilenameWithoutExtension();
            var msbuildPathFilePath = new FilePath(string.Format("{0}/{1}/{1}.csproj", BuildParameters.SourceDirectoryPath.FullPath, fileName));

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

            if (FileExists(msbuildPathFilePath))
            {
                settings.MSBuildProjectPath = msbuildPathFilePath;
            }

            if (Context.Log.Verbosity == Verbosity.Verbose || Context.Log.Verbosity == Verbosity.Diagnostic)
            {
                settings.Statistics = true;
            }

            EazfuscatorNet(file, settings);
        }
    }
    else
    {
        Information("There are no files defined to be obfuscated.");
    }
});
