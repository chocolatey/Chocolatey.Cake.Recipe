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

public class BuildVersion
{
    public string MajorMinorPatch { get; private set; }
    public string SemVersion { get; private set; }
    public string Milestone { get; private set; }
    public string CakeVersion { get; private set; }
    public string FileVersion { get; private set;}
    public string PackageVersion { get; private set; }
    public string InformationalVersion { get; private set; }
    public string FullSemVersion { get; private set; }

    public static BuildVersion CalculatingSemanticVersion(
        ICakeContext context,
        string preReleaseLabelFilePath)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

        if (!BuildParameters.ShouldRunGitVersion)
        {
            context.Information("Running GitVersion is not enabled, so returning default values...");

            return new BuildVersion
            {
                MajorMinorPatch = "0.1.0",
                SemVersion = "0.1.0-alpha.0",
                Milestone = "0.1.0",
                CakeVersion = cakeVersion,
                FileVersion = "0.1.0.0",
                PackageVersion = "0.1.0-alpha-20220317-13",
                InformationalVersion = "0.1.0-alpha.0+Branch.develop.Sha.528f9bf572a52f0660cbe3f4d109599eab1e9866",
                FullSemVersion = "0.1.0-alpha.0",
            };
        }

        try
        {
            context.Information("Testing to see if valid git repository...");

            var rootPath = BuildParameters.RootDirectoryPath;
            rootPath = context.GitFindRootFromPath(rootPath);
        }
        catch (LibGit2Sharp.RepositoryNotFoundException)
        {
            context.Warning("Unable to locate git repository, so GitVersion can't be executed, returning default version numbers...");

            return new BuildVersion
            {
                MajorMinorPatch = "0.1.0",
                SemVersion = "0.1.0-alpha.0",
                Milestone = "0.1.0",
                CakeVersion = cakeVersion,
                FileVersion = "0.1.0.0",
                PackageVersion = "0.1.0-alpha-20220317-13",
                InformationalVersion = "0.1.0-alpha.0+Branch.develop.Sha.528f9bf572a52f0660cbe3f4d109599eab1e9866",
                FullSemVersion = "0.1.0-alpha.0",
            };
        }

        string majorMinorPatch = null;
        string semVersion = null;
        string milestone = null;
        string fileVersion = null;
        string packageVersion = null;
        string informationalVersion = null;
        string fullSemVersion = null;

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
        semVersion = assertedVersions.LegacySemVerPadded;
        // Milestone is kep as a separate variable, since it _can_ be prepended with a "v", which is something that we might look at
        milestone = majorMinorPatch;
        fileVersion = assertedVersions.AssemblySemVer;
        informationalVersion = assertedVersions.InformationalVersion;
        fullSemVersion = assertedVersions.FullSemVer;

        prerelease = assertedVersions.PreReleaseLabel;

        prerelease = prerelease.Replace("-", string.Empty);

        // Chocolatey doesn't support a prerelease that starts with a digit.
        // If we see a digit here, merely replace it with an `a` to get around this.
        if (System.Text.RegularExpressions.Regex.Match(prerelease, @"^\d.*$").Success)
        {
            prerelease = string.Format("a{0}", prerelease);
        }

        // Having a pre-release label of greater than 10 characters can cause problems when trying to run choco pack.
        // Since we typically only see this when building a local feature branch, or a PR, let's just trim it down to
        // the 10 character limit, and move on.
        if (prerelease.Length > 10)
        {
            prerelease = prerelease.Substring(0, 10);
        }

        sha = assertedVersions.Sha.Substring(0,8);

        if (context.FileExists(preReleaseLabelFilePath))
        {
            prerelease = System.IO.File.ReadAllText(preReleaseLabelFilePath);
        }

        var buildDate = string.Format("-{0}", DateTime.Now.ToString("yyyyMMdd"));

        // Don't include the build date in the package version number, if not on an alpha or beta branch.
        // This should allow packaging of Chocolatey package to work without erroring.
        if (!BuildParameters.IsTagged)
        {
            packageVersion = string.Format("{0}-{1}{2}{3}",
                                majorMinorPatch,
                                prerelease,
                                prerelease == "alpha" || prerelease == "beta" ? buildDate : string.Empty,
                                BuildParameters.BuildCounter != "-1" ? string.Format("-{0}", BuildParameters.BuildCounter) : string.Empty);
            informationalVersion = string.Format("{0}-{1}{2}-{3}", majorMinorPatch, prerelease, buildDate, sha);
            context.Information("There is no tag.");
        }
        else
        {
            packageVersion = semVersion;
            informationalVersion = semVersion;
            context.Information("There is a tag.");
        }

        if (BuildParameters.ShouldGenerateSolutionVersionCSharpFile)
        {
            context.Information("Generating SolutionVersion.cs file...");

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
                        Value = BuildParameters.StrongNameKeyPath.Replace("\\", "\\\\"),
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
        }
        else
        {
            context.Information("Skipping generation of SolutionVersion.cs file.");
        }

        context.Information("Calculated Major.Minor.Patch: {0}", majorMinorPatch);
        context.Information("Calculated Sem Version: {0}", semVersion);
        context.Information("Calculated Milestone: {0}", milestone);
        context.Information("Cake Version: {0}", cakeVersion);
        context.Information("Calculated File Version: {0}", fileVersion);
        context.Information("Calculated Package Version: {0}", packageVersion);
        context.Information("Calculated Informational Version: {0}", informationalVersion);
        context.Information("Calculate Full Sem Version: {0}", fullSemVersion);

        if (context.BuildSystem().IsRunningOnTeamCity)
        {
            // use the asserted package version for the build number in TeamCity
            context.BuildSystem().TeamCity.SetBuildNumber(packageVersion);
        }

        return new BuildVersion
        {
            MajorMinorPatch = majorMinorPatch,
            SemVersion = semVersion,
            Milestone = milestone,
            CakeVersion = cakeVersion,
            FileVersion = fileVersion,
            PackageVersion = packageVersion,
            InformationalVersion = informationalVersion,
            FullSemVersion = fullSemVersion,
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
