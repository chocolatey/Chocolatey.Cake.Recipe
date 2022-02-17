BuildParameters.Tasks.StrongNameSignerTask = Task("Strong-Name-Signer")
    .WithCriteria(() => BuildParameters.ShouldStrongNameSignDependentAssemblies, "Skipping since strong name signing of dependent assemblies has been disabled")
    .Does(() => RequireTool(ToolSettings.StrongNameSignerTool, () => {
        var settings = new StrongNameSignerSettings();
        settings.KeyFile = BuildParameters.StrongNameKeyPath;
        settings.InputDirectory = BuildParameters.StrongNameDependentAssembliesInputPath;
        settings.LogLevel = StrongNameSignerVerbosity.Summary;

        StrongNameSigner(settings);
    })
);

// TODO: If/when a Cake.SNRemove addin is created, this won't be required, since the SNRemove.exe will be embedded
// into the addin package
BuildParameters.Tasks.InstallSNRemoveTask = Task("Install-SNRemove")
    .Does(() => {
        var snRemoveDirectoryPath = Context.Configuration.GetToolPath("/", Context.Environment).Combine("SNRemove");
        var snRemoveZipFilePath = snRemoveDirectoryPath.CombineWithFilePath("snremove.zip");
        var snRemoveExeFilePath = snRemoveDirectoryPath.CombineWithFilePath("snremove.exe");

        if (!DirectoryExists(snRemoveDirectoryPath))
        {
            CleanDirectory(snRemoveDirectoryPath);
        }

        if (!FileExists(snRemoveZipFilePath))
        {
            DownloadFile("https://www.nirsoft.net/dot_net_tools/snremove.zip", snRemoveZipFilePath);
        }

        if (FileExists(snRemoveZipFilePath) && !FileExists(snRemoveExeFilePath))
        {
            Unzip(snRemoveZipFilePath, snRemoveDirectoryPath);
        }

        Context.Tools.RegisterFile(snRemoveExeFilePath);
    });

    // TODO: This class can be removed once a PR into the Cake.StrongNameTool project is created and accepted
    public sealed class StrongNameToolResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICakeEnvironment _environment;
        private readonly IRegistry _registry;
        private FilePath _strongnameToolPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cake.StrongNameTool.StrongNameResolver"/> class.
        /// </summary>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="registry">The registry.</param>
        public StrongNameToolResolver(IFileSystem fileSystem, ICakeEnvironment environment, IRegistry registry)
        {
            _fileSystem = fileSystem;
            _environment = environment;
            _registry = registry;

            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }
            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }
        }

        /// <summary>
        /// Resolves the path to the strong name tool (sn.exe)
        /// </summary>
        /// <returns>The path to sn.exe</returns>
        public FilePath GetPath()
        {
            if (_strongnameToolPath != null)
            {
                return _strongnameToolPath;
            }

            _strongnameToolPath = GetFromDisc() ?? GetFromRegistry();
            if (_strongnameToolPath == null)
            {
                throw new CakeException("Failed to find sn.exe.");
            }

            return _strongnameToolPath;
        }

        /// <summary>
        /// Gets the path to sn.exe from disc.
        /// </summary>
        /// <returns>The path to sn.exe from disc.</returns>
        private FilePath GetFromDisc()
        {
            // Get the path to program files.
            var programFilesPath = _environment.GetSpecialPath(SpecialPath.ProgramFilesX86);

            var possibleVersions = new[] { "v10.0A", "v8.1A", "v8.1", "v8.0", "v7.0A" };

            // Get a list of the files we should check.
            var files = new List<FilePath>();
            if (_environment.Platform.Is64Bit)
            {
                // 64-bit specific paths.
                foreach (var version in possibleVersions)
                {
                    //NETFX4
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\NETFX 4.0 Tools\x64\", version)).CombineWithFilePath("sn.exe"));
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\NETFX 4.8 Tools\x64\", version)).CombineWithFilePath("sn.exe"));
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\x64\", version)).CombineWithFilePath("sn.exe"));
                }
            }
            else
            {
                // 32-bit specific paths.
                foreach (var version in possibleVersions)
                {
                    //NETFX4
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\NETFX 4.0 Tools\", version)).CombineWithFilePath("sn.exe"));
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\NETFX 4.8 Tools\", version)).CombineWithFilePath("sn.exe"));
                    files.Add(programFilesPath.Combine(string.Format(@"Microsoft SDKs\Windows\{0}\Bin\", version)).CombineWithFilePath("sn.exe"));
                }
            }

            // Return the first path that exist.
            return files.FirstOrDefault(file => _fileSystem.Exist(file));
        }

        /// <summary>
        /// Gets the installation folder of sn.exe from the registry.
        /// </summary>
        /// <returns>The install folder to sn.exe from registry.</returns>
        private FilePath GetFromRegistry()
        {
            using (var root = _registry.LocalMachine.OpenKey("Software\\Microsoft\\Microsoft SDKs\\Windows"))
            {
                if (root == null)
                {
                    return null;
                }

                var keyName = root.GetSubKeyNames();
                foreach (var key in keyName)
                {
                    var sdkKey = root.OpenKey(key);
                    if (sdkKey != null)
                    {
                        IRegistryKey fxKey;
                        if (_environment.Platform.Is64Bit)
                        {
                            fxKey = sdkKey.OpenKey("WinSDK-NetFx40Tools-x64");
                        }
                        else
                        {
                            fxKey = sdkKey.OpenKey("WinSDK-NetFx40Tools");
                        }

                        if (fxKey != null)
                        {
                            var installationFolder = fxKey.GetValue("InstallationFolder") as string;
                            if (!string.IsNullOrWhiteSpace(installationFolder))
                            {
                                var installationPath = new DirectoryPath(installationFolder);
                                var signToolPath = installationPath.CombineWithFilePath("sn.exe");

                                if (_fileSystem.Exist(signToolPath))
                                {
                                    return signToolPath;
                                }
                            }
                        }
                        else
                        {
                            // if NETFX4 isn't present
                            var installationFolder = sdkKey.GetValue("CurrentInstallFolder") as string;
                            if (!string.IsNullOrEmpty(installationFolder))
                            {
                                var installationPath = new DirectoryPath(installationFolder);
                                var signToolPath = installationPath.CombineWithFilePath("sn.exe");

                                if (_fileSystem.Exist(signToolPath))
                                {
                                    return signToolPath;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }

    public string GetAssemblyPublicKeyToken(string pathToAssembly)
    {
        IEnumerable<string> redirectedStandardOutput;
        IEnumerable<string> redirectedError;

        var resolver = new StrongNameToolResolver(Context.FileSystem, Context.Environment, Context.Registry);
        var strongNameToolPath = resolver.GetPath();

        var exitCode = StartProcess(
            strongNameToolPath,
            new ProcessSettings {
                Arguments = string.Format("-q -T {0}", pathToAssembly),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            out redirectedStandardOutput,
            out redirectedError
        );

        if (exitCode == 0)
        {
            var regexPattern = new System.Text.RegularExpressions.Regex("(Public Key Token: |Public key token is )(.*)");
            var matches = regexPattern.Matches(redirectedStandardOutput.ToList()[0]);

            if(matches.Count == 1)
            {
                return matches[0].Groups[2].Value;
            }
        }

        return null;
    }

    // TODO: If/when a Cake.SNRemove addin is created, this can be simplified
    public void RemoveStrongNameSigning(string pathToAssembly)
    {
        var filePath = new FilePath(pathToAssembly);
        var assemblyName = filePath.GetFilename();

        if (!BuildParameters.AllowedAssemblyNames.Contains(assemblyName.FullPath))
        {
            Information("\tUnable to remove Strong Name from assembly {0} as it isn't on the allowed list.", assemblyName);
        }
        else
        {
            var snRemoveExeFilePath = Context.Tools.Resolve("snremove.exe");

            var exitCode = StartProcess(
                snRemoveExeFilePath,
                new ProcessSettings {
                    Arguments = string.Format("-r {0}", pathToAssembly.Replace("/", "\\")),
                }
            );

            if (exitCode != 0)
            {
                Error("\tUnable to remove strong name signing from assembly: {0}", pathToAssembly);
            }
        }
    }

    public void TestAndUpdateAssembliesPublicKeyToken(string requiredPublicKeyToken, List<string> pathsToAssemblyDirectories)
    {
        var assemblyDirectories = new List<string>();

        foreach (var pathToAssemblyDirectory in pathsToAssemblyDirectories)
        {
            foreach (var assemblyPath in GetFiles(pathToAssemblyDirectory + "/**/*.dll"))
            {
                var currentAssemblyPublicKeyToken = GetAssemblyPublicKeyToken(assemblyPath.FullPath);
                Information("\tPublic Key Token of {0}: {1}", assemblyPath, currentAssemblyPublicKeyToken);

                if (currentAssemblyPublicKeyToken != requiredPublicKeyToken)
                {
                    var assemblyDirectory = ((FilePath)assemblyPath).GetDirectory().FullPath;
                    if (!assemblyDirectories.Contains(assemblyDirectory))
                    {
                       assemblyDirectories.Add(assemblyDirectory);
                    }

                    Information("\tPublic Key Tokens are not the same.  Removing current Public Key Token...");
                    RemoveStrongNameSigning(assemblyPath.FullPath);
                }
                else
                {
                    Information("\tPublic Key Tokens match, so nothing to do here.");
                }
            }
        }

        if (assemblyDirectories.Any())
        {
            Information("\tStrong Name Signing Assemblies...");

            // So that dependent assembly references are also correctly updated, it is necessary to use the InputDirectory
            // argument, rather than the AssemblyFile argument.  See here:
            // https://github.com/brutaldev/StrongNameSigner#dealing-with-dependencies
            // for more information.
            var delimitedStringOfPaths = string.Join("|", assemblyDirectories);
            Information("Delimited string of Assembly Paths: {0}", delimitedStringOfPaths);
            var settings = new StrongNameSignerSettings();
            settings.KeyFile = BuildParameters.StrongNameKeyPath;
            settings.InputDirectory = delimitedStringOfPaths;
            settings.LogLevel = StrongNameSignerVerbosity.Summary;

            StrongNameSigner(settings);

            Information("\tAssemblies Updated with required Public Key Token.");
        }
    }

BuildParameters.Tasks.ChangeStrongNameSignatures = Task("Change-Strong-Name-Signatures")
    .IsDependentOn("Install-SNRemove")
    .WithCriteria(() => BuildParameters.ShouldStrongNameChocolateyDependenciesWithCurrentPublicKeyToken, "Skipping since strong naming of Chocolatey dependencies has been disabled")
    .Does(() => RequireTool(ToolSettings.StrongNameSignerTool, () => {
        var requiredPublicKeyToken = BuildParameters.StrongNameKeyPath.Contains("official") ? "79d02ea9cad655eb" : "fd112f53c3ab578c";

        if (BuildParameters.UseChocolateyGuiStrongNameKey)
        {
            requiredPublicKeyToken = BuildParameters.StrongNameKeyPath.Contains("official") ? "dfd1909b30b79d8b" : "ffc115b9f4eb5c26";
        }

        Information("Required Public Key Token: {0}", requiredPublicKeyToken);

        // By default, Referenced Packages that will need to be updated
        // - chocolatey.lib
        // - chocolatey-licensed.lib
        // - ChocolateyGui.Common
        // - ChocolateyGui.Common.Windows

        // Need to loop through all csproj and packages.config files and check to see if there are
        // any references to the above packages.  If there are, find out the version
        // number, and then update all assemblies
        var packageReferenceFiles = GetFiles(BuildParameters.SourceDirectoryPath + "/**/packages.config");
        var regularExpressionPattern = string.Format("<package id=\"({0})\" version=\"(\\S*)\" .*/>", BuildParameters.AssemblyNamesRegexPattern);
        var formatReplacement = "{0}/packages/{1}.{2}/lib";

        if (BuildParameters.IsDotNetCoreBuild)
        {
            packageReferenceFiles = GetFiles(BuildParameters.SourceDirectoryPath + "/**/*.csproj");
            regularExpressionPattern = string.Format("<PackageReference Include=\"({0})\" Version=\"(\\S*)\" .*/>", BuildParameters.AssemblyNamesRegexPattern);
            formatReplacement = "{0}/packages/{1}/{2}/lib";
        }

        var processedMatches = new List<string>();

        foreach (var packageReferenceFile in packageReferenceFiles)
        {
            var packageMatches = FindRegexMatchesGroupsInFile(packageReferenceFile, regularExpressionPattern, System.Text.RegularExpressions.RegexOptions.None);

            foreach (var packageMatch in packageMatches)
            {
                var packageMatchGroups = packageMatch.ToList();
                var packageName = packageMatchGroups[1].Captures[0].Value;
                var packageVersion = packageMatchGroups[2].Captures[0].Value;
                var potentialReferencedDirectory = MakeAbsolute((DirectoryPath)string.Format(formatReplacement, BuildParameters.SourceDirectoryPath.FullPath, packageName, packageVersion));

                if (!processedMatches.Contains(potentialReferencedDirectory.FullPath))
                {
                    Information("Found Directory that hasn't been processed yet: {0}", potentialReferencedDirectory);
                    processedMatches.Add(potentialReferencedDirectory.FullPath);
                }
                else
                {
                    Information("Already processed Directory {0}, moving on.", potentialReferencedDirectory);
                }
            }
        }

        Information("Testing and updating assemblies to required Public Key Token...");

        TestAndUpdateAssembliesPublicKeyToken(requiredPublicKeyToken, processedMatches);
    })
);
