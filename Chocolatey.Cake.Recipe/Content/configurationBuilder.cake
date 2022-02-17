using Newtonsoft.Json.Linq;

BuildParameters.Tasks.ConfigurationBuilderTask = Task("Configuration-Builder")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var globberSettings = new GlobberSettings();
    globberSettings.Predicate = new Func<IDirectory, bool>(d => !d.Path.FullPath.Contains("node_modules") && !d.Path.FullPath.Contains("code_drop") && !d.Path.FullPath.Contains("bin"));
    var settingsFilePaths = GetFiles(MakeAbsolute(BuildParameters.Paths.Directories.EnvironmentSettings) + "/**/*.settings");

    foreach (var settingsFilePath in settingsFilePaths)
    {
        var settingsFileName = settingsFilePath.GetFilenameWithoutExtension();
        Verbose(" - Settings Name: {0}", settingsFileName);

        if (settingsFileName.FullPath.ToLower() == BuildParameters.DeploymentEnvironment.ToLower())
        {
            var settingsJson = DeserializeJsonFromFile<Transformations>(settingsFilePath.FullPath);

            foreach (var transform in settingsJson.transforms)
            {
                if (FileExists(MakeAbsolute((FilePath)transform.path)))
                {
                    var updates = new TransformationCollection();
                    foreach (var update in transform.updates)
                    {
                        updates.Add(update.Key, update.Value);
                    }

                    Information("Updates to perform {0}...", SerializeJson(updates));
                    TransformConfig(transform.path, transform.path, updates);
                }
            }
        }
    }
});

public class Transformations
{
    public Transform[] transforms { get; set; }
}

public class Transform
{
    public string path { get; set; }
    public Dictionary<string, string> updates { get; set; }
}