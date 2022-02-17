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

            settings.ToolPath = "./lib/Eazfuscator.NET/Eazfuscator.NET.exe";

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
