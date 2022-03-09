public class BuildVersion
{
    public string Version { get; private set; }
    public string SemVersion { get; private set; }
    public string Milestone { get; private set; }
    public string CakeVersion { get; private set; }
    public string InformationalVersion { get; private set; }
    public string FullSemVersion { get; private set; }
    public string PackageVersion { get; private set; }

    public static BuildVersion CalculatingSemanticVersion(
        ICakeContext context,
        string preReleaseLabelFilePath)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string majorMinorPatch = null;
        string version = null;
        string fileVersion = null;
        string semVersion = null;
        string milestone = null;
        string informationalVersion = null;
        string fullSemVersion = null;
        string packageVersion = null;
        string prerelease = null;
        string sha = null;
        GitVersion assertedVersions = null;

        if (BuildParameters.BuildAgentOperatingSystem != PlatformFamily.Windows)
        {
            PatchGitLibConfigFiles(context);
        }

        context.Information("Calculating Semantic Version...");

        if (!BuildParameters.IsLocalBuild)
        {
            context.Information("Running GitVersion with BuildServer flag...");
            context.GitVersion(new GitVersionSettings{
                OutputType = GitVersionOutput.BuildServer,
                NoFetch = true,
                ArgumentCustomization = args => args.Append("/nocache")
            });

            assertedVersions = context.GitVersion(new GitVersionSettings{
                OutputType = GitVersionOutput.Json,
                NoFetch = true,
                ArgumentCustomization = args => args.Append("/nocache")
            });
        }
        else
        {
            context.Information("Running GitVersion directly...");
            assertedVersions = context.GitVersion(new GitVersionSettings{
                OutputType = GitVersionOutput.Json,
                ArgumentCustomization = args => args.Append("/nocache")
            });
        }

        majorMinorPatch = assertedVersions.MajorMinorPatch;
        fileVersion = assertedVersions.AssemblySemVer;
        semVersion = assertedVersions.LegacySemVerPadded;
        informationalVersion = assertedVersions.InformationalVersion;
        milestone = string.Concat(version);
        fullSemVersion = assertedVersions.FullSemVer;
        prerelease = assertedVersions.PreReleaseLabel;
        sha = assertedVersions.Sha.Substring(0,8);

        var preReleaseLabel = string.Empty;

        if (context.FileExists(preReleaseLabelFilePath))
        {
            preReleaseLabel = System.IO.File.ReadAllText(preReleaseLabelFilePath);
        }

        var buildDate = DateTime.Now.ToString("yyyyMMdd");

        if (!BuildParameters.IsTagged)
        {
            packageVersion = string.Format("{0}-{1}-{2}{3}", majorMinorPatch, string.IsNullOrWhiteSpace(preReleaseLabel) ? prerelease : preReleaseLabel, buildDate, BuildParameters.BuildCounter != "-1" ? string.Format("-{0}", BuildParameters.BuildCounter) : string.Empty);
            informationalVersion = string.Format("{0}-{1}-{2}-{3}", majorMinorPatch, string.IsNullOrWhiteSpace(preReleaseLabel) ? prerelease : preReleaseLabel, buildDate, sha);
            context.Information("There is no tag.");
        }
        else
        {
            packageVersion = semVersion;
            informationalVersion = semVersion;
            context.Information("There is a tag.");
        }

        // create SolutionVersion.cs file...
        var assemblyInfoSettings = new AssemblyInfoSettings {
            ComVisible = BuildParameters.ProductComVisible,
            CLSCompliant = BuildParameters.ProductClsCompliant,
            Company = BuildParameters.ProductCompany,
            Version = fileVersion,
            FileVersion = fileVersion,
            InformationalVersion = informationalVersion,
            Product = BuildParameters.ProductName,
            Description = BuildParameters.ProductDescription,
            Trademark = BuildParameters.ProductTrademark,
            Copyright = BuildParameters.ProductCopyright
        };

        if (BuildParameters.ProductCustomAttributes != null)
        {
            assemblyInfoSettings.CustomAttributes = BuildParameters.ProductCustomAttributes;
        }
        else
        {
            assemblyInfoSettings.CustomAttributes = new List<AssemblyInfoCustomAttribute>();

            if (BuildParameters.ShouldStrongNameOutputAssemblies)
            {
                var assemblyKeyFileAttribute = new AssemblyInfoCustomAttribute
                {
                    Name = "AssemblyKeyFile",
                    Value = BuildParameters.StrongNameKeyPath.Replace("\\", "\\\\").Replace("/", "\\\\"),
                    NameSpace = "System.Reflection"
                };

                assemblyInfoSettings.CustomAttributes.Add(assemblyKeyFileAttribute);
            }

            var obfuscateAssemblyAttribute = new AssemblyInfoCustomAttribute
            {
                Name = "ObfuscateAssembly",
                Value = BuildParameters.ObfuscateAssembly,
                NameSpace = "System.Reflection"
            };

            assemblyInfoSettings.CustomAttributes.Add(obfuscateAssemblyAttribute);
        }

        context.CreateAssemblyInfo(BuildParameters.Paths.Files.SolutionInfoFilePath, assemblyInfoSettings);

        context.Information("Calculated File Version: {0}", fileVersion);
        context.Information("Calculated Package Version: {0}", packageVersion);
        context.Information("Calculated Informational Version: {0}", informationalVersion);

        var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

        if (context.BuildSystem().IsRunningOnTeamCity)
        {
            // use the asserted package version for the build number in TeamCity
            context.BuildSystem().TeamCity.SetBuildNumber(packageVersion);

            if (BuildParameters.Title == "ChocolateySoftware.ChocolateyManagement")
            {
                // Only set this when it is a CCM Build
                context.BuildSystem().TeamCity.SetParameter("CCMVersion", packageVersion);
            }
        }

        return new BuildVersion
        {
            Version = majorMinorPatch,
            SemVersion = semVersion,
            Milestone = milestone,
            CakeVersion = cakeVersion,
            InformationalVersion = informationalVersion,
            FullSemVersion = fullSemVersion,
            PackageVersion = packageVersion
        };
    }

    private static void PatchGitLibConfigFiles(ICakeContext context)
    {
        var configFiles = context.GetFiles("./tools/**/LibGit2Sharp.dll.config");
        var libgitPath = GetLibGit2Path(context);
        if (string.IsNullOrEmpty(libgitPath)) { return; }

        foreach (var config in configFiles) {
            var xml = System.Xml.Linq.XDocument.Load(config.ToString());

            if (xml.Element("configuration").Elements("dllmap")
                .All(e => e.Attribute("target").Value != libgitPath)) {

                var dllName = xml.Element("configuration").Elements("dllmap").First(e => e.Attribute("os").Value == "linux").Attribute("dll").Value;
                xml.Element("configuration")
                    .Add(new System.Xml.Linq.XElement("dllmap",
                        new System.Xml.Linq.XAttribute("os", "linux"),
                        new System.Xml.Linq.XAttribute("dll", dllName),
                        new System.Xml.Linq.XAttribute("target", libgitPath)));

                context.Information($"Patching '{config}' to use fallback system path on Linux...");
                xml.Save(config.ToString());
            }
        }
    }

    private static string GetLibGit2Path(ICakeContext context)
    {
        var possiblePaths = new[] {
            "/usr/lib*/libgit2.so*",
            "/usr/lib/*/libgit2.so*"
        };

        foreach (var path in possiblePaths) {
            var file = context.GetFiles(path).FirstOrDefault();
            if (file != null && !string.IsNullOrEmpty(file.ToString())) {
                return file.ToString();
            }
        }

        return null;
    }
}
