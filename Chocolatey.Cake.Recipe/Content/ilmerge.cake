BuildParameters.Tasks.ILMergeTask = Task("Run-ILMerge")
    .IsDependeeOf("Copy-Nuspec-Folders")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .Does(() => RequireTool(ToolSettings.ILMergeTool, () =>
{
    if (BuildParameters.GetILMergeConfigs != null)
    {
        foreach (var ilMergeConfig in BuildParameters.GetILMergeConfigs())
        {
            var outputDirectory = ilMergeConfig.Output.GetDirectory();
            CleanDirectory(outputDirectory);

            var settings = new ILMergeSettings {
                ArgumentCustomization = args=>args.Prepend(string.Format("/allowDup /targetplatform:\"{0}\" /target:{1} /internalize:{2} /keyfile:{3} /log:{4}", ilMergeConfig.TargetPlatform, ilMergeConfig.Target, ilMergeConfig.Internalize, ilMergeConfig.KeyFile, ilMergeConfig.LogFile))
            };

            Information("Running ILMerge...");
            ILMerge(ilMergeConfig.Output, ilMergeConfig.PrimaryAssemblyName, ilMergeConfig.AssemblyPaths, settings);
        
            if (FileExists(ilMergeConfig.LogFile))
            {
                BuildParameters.BuildProvider.UploadArtifact(ilMergeConfig.LogFile);
            }
        }
    }
    else
    {
        Information("There are no ILMerge Configs defined for this build.");
    }
}));

public class ILMergeConfig
{
    public FilePath Internalize { get; set; }
    public string TargetPlatform { get; set; }
    public string Target { get; set; }
    public FilePath Output { get; set; }
    public FilePath KeyFile { get; set; }
    public FilePath LogFile { get; set; }
    public bool AllowDuplicate { get; set; }
    public string PrimaryAssemblyName { get; set; }
    public FilePathCollection AssemblyPaths { get; set; }

    public ILMergeConfig()
    {

    }

    public ILMergeConfig(FilePath internalize,
    string targetPlatform,
    string target,
    FilePath output,
    FilePath keyFile,
    FilePath logFile,
    bool allowDuplicate,
    string primaryAssemblyName,
    FilePathCollection assemblyPaths)
    {
        Internalize = internalize;
        TargetPlatform = targetPlatform;
        Target = target;
        Output = output;
        KeyFile = keyFile;
        LogFile = logFile;
        AllowDuplicate = allowDuplicate;
        PrimaryAssemblyName = primaryAssemblyName;
        AssemblyPaths = assemblyPaths;
    }
}