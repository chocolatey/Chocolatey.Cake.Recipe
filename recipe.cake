#load nuget:?package=Cake.Recipe&version=1.0.0

Environment.SetVariableNames();

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./src",
                            title: "Chocolatey.Cake.Recipe",
                            repositoryOwner: "chocolatey",
                            repositoryName: "Chocolatey.Cake.Recipe",
                            appVeyorAccountName: "chocolatey",
                            shouldRunGitVersion: true,
                            shouldDeployGraphDocumentation: false,
                            nuspecFilePath: "./Chocolatey.Cake.Recipe/Chocolatey.Cake.Recipe.nuspec");

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(context: Context);

BuildParameters.Tasks.CleanTask
    .IsDependentOn("Generate-Version-File");

Task("Generate-Version-File")
    .Does(() => {
        var buildMetaDataCodeGen = TransformText(@"
        public class BuildMetaData
        {
            public static string Date { get; } = ""<%date%>"";
            public static string Version { get; } = ""<%version%>"";
        }",
        "<%",
        "%>"
        )
   .WithToken("date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
   .WithToken("version", BuildParameters.Version.SemVersion)
   .ToString();

    System.IO.File.WriteAllText(
        "./Chocolatey.Cake.Recipe/Content/version.cake",
        buildMetaDataCodeGen
        );
    });

Build.RunNuGet();
