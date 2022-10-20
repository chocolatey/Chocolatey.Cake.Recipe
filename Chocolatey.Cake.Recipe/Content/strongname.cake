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