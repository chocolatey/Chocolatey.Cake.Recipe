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

BuildParameters.Tasks.VerifyPowerShellScriptsTask = Task("Verify-PowerShellScripts")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => !BuildParameters.IsPullRequest, "Skipping because current build is from a Pull Request")
    .WithCriteria(() => BuildParameters.BranchType == BranchType.Master || BuildParameters.BranchType == BranchType.Release || BuildParameters.BranchType == BranchType.HotFix || BuildParameters.BranchType == BranchType.Support || BuildParameters.BranchType == BranchType.Develop, "Skipping because this is not a 'main' branch, i.e. master, develop, release, hotfix, or support, where scripts need to be verified.")
    .WithCriteria(() => BuildParameters.ShouldVerifyPowerShellScripts, "Skipping since verifying PowerShell scripts has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetScriptsToVerify != null)
    {
        var powerShellVerifyScript = GetFiles("./tools/Chocolatey.Cake.Recipe*/Content/verify-powershell.ps1").FirstOrDefault();

        if (powerShellVerifyScript == null)
        {
            Warning("Unable to find PowerShell verification script, so unable to verify PowerShell scripts.");
            return;
        }

        var scriptsToVerify = new List<string>();
        foreach (var filePath in BuildParameters.GetScriptsToVerify())
        {
            scriptsToVerify.Add(MakeAbsolute(filePath).FullPath);
        }

        if (scriptsToVerify.Count == 0)
        {
            Information("There are no PowerShell Scripts defined to be verified.");
            return;
        }

        StartPowershellFile(MakeAbsolute(powerShellVerifyScript), args =>
        {
            args.AppendArray("ScriptsToVerify", scriptsToVerify);
        });
    }
    else
    {
        Information("There are no PowerShell Scripts defined to be verified.");
    }
});

BuildParameters.Tasks.SignPowerShellScriptsTask = Task("Sign-PowerShellScripts")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => (!string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath)) || BuildSystem.IsRunningOnTeamCity, "Skipping because unable to find certificate, and not running on TeamCity")
    .WithCriteria(() => BuildParameters.ShouldAuthenticodeSignPowerShellScripts, "Skipping since authenticode signing of PowerShell scripts has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetScriptsToSign != null)
    {
        var powerShellSigningScript = GetFiles("./tools/Chocolatey.Cake.Recipe*/Content/sign-powershell.ps1").FirstOrDefault();

        if (powerShellSigningScript == null)
        {
            Warning("Unable to find PowerShell signing script, so unable to sign PowerShell scripts.");
            return;
        }

        var scriptsToSign = new List<string>();
        foreach (var filePath in BuildParameters.GetScriptsToSign())
        {
            scriptsToSign.Add(MakeAbsolute(filePath).FullPath);
        }

        if (scriptsToSign.Count == 0)
        {
            Warning("Unable to find PowerShell signing script, so unable to sign PowerShell scripts.");
            return;
        }

        if (BuildSystem.IsRunningOnTeamCity)
        {
            StartPowershellFile(MakeAbsolute(powerShellSigningScript), args =>
            {
                args.AppendArray("ScriptsToSign", scriptsToSign)
                    .Append("OutputFolder", BuildParameters.Paths.Directories.SignedFiles.FullPath)
                    .Append("TimeStampServer", BuildParameters.CertificateTimestampUrl)
                    .AppendQuoted("CertificateSubjectName", BuildParameters.CertificateSubjectName)
                    .Append("CertificateAlgorithm", BuildParameters.CertificateAlgorithm);
            });
        }
        else
        {
            var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

            Information("Signing above scripts with {0}", BuildParameters.CertificateFilePath);

            StartPowershellFile(MakeAbsolute(powerShellSigningScript), args =>
                {
                    args.AppendArray("ScriptsToSign", scriptsToSign)
                        .Append("OutputFolder", BuildParameters.Paths.Directories.SignedFiles.FullPath)
                        .Append("TimeStampServer", BuildParameters.CertificateTimestampUrl)
                        .Append("CertificatePath", BuildParameters.CertificateFilePath)
                        .AppendSecret("CertificatePassword", password)
                        .Append("CertificateAlgorithm", BuildParameters.CertificateAlgorithm);
                });
        }

        var files = GetFiles(BuildParameters.Paths.Directories.SignedFiles + "/**/*") - GetFiles(BuildParameters.Paths.Directories.SignedFiles + "/**/*.zip");
        var destination = BuildParameters.Paths.Directories.SignedFiles.CombineWithFilePath("SignedFiles.zip");

        if (files.Count == 0)
        {
            Information("No PowerShell scripts found, or all PowerShell scripts have already been signed.");
            return;
        }


        Zip(BuildParameters.Paths.Directories.SignedFiles, destination, files);

        if (!FileExists(destination))
        {
            Information("Signed script archive was not created. Ensure there were signed scripts and the archive path is accessible.");
            return;
        }

        BuildParameters.BuildProvider.UploadArtifact(destination);
    }
    else
    {
        Information("There are no PowerShell Scripts defined to be signed.");
    }
});

BuildParameters.Tasks.SignAssembliesTask = Task("Sign-Assemblies")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => (!string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath)) || BuildSystem.IsRunningOnTeamCity, "Skipping because unable to find certificate, and not running on TeamCity")
    .WithCriteria(() => BuildParameters.ShouldAuthenticodeSignOutputAssemblies, "Skipping since authenticode signing of output assemblies has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetFilesToSign != null)
    {
        foreach (var fileToSign in BuildParameters.GetFilesToSign())
        {
            if (BuildSystem.IsRunningOnTeamCity)
            {
                Information("Registering SignTool custom location...");

                // TODO: Let's not hard code this...
                Context.Tools.RegisterFile("C:\\Program Files\\Microsoft SDKs\\Windows\\v7.1\\Bin\\signtool.exe");

                Information("Signing '{0}' using Certificate Subject Name {1}", fileToSign, BuildParameters.CertificateSubjectName);

                Sign(fileToSign, new SignToolSignSettings {
                        TimeStampUri = new Uri(BuildParameters.CertificateTimestampUrl),
                        UseMachineStore = true,
                        CertSubjectName = BuildParameters.CertificateSubjectName,
                        DigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true),
                        TimeStampDigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true)
                    });
            }
            else
            {
                Information("Signing '{0}' with {1}", fileToSign, BuildParameters.CertificateFilePath);

                var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

                Sign(fileToSign, new SignToolSignSettings {
                        TimeStampUri = new Uri(BuildParameters.CertificateTimestampUrl),
                        CertPath = BuildParameters.CertificateFilePath,
                        Password = password,
                        DigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true),
                        TimeStampDigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true)
                    });
            }
        }
    }
    else
    {
        Information("There are no assemblies defined to be signed.");
    }
});

BuildParameters.Tasks.SignMsisTask = Task("Sign-Msis")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping due to not running on Windows")
    .WithCriteria(() => (!string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath)) || BuildSystem.IsRunningOnTeamCity, "Skipping because unable to find certificate, and not running on TeamCity")
    .WithCriteria(() => BuildParameters.ShouldAuthenticodeSignMsis, "Skipping since authenticode signing of msi's has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetMsisToSign != null)
    {
        // dual signing Sha1 and Sha256 for an MSI would require https://github.com/puppetlabs/packaging/blob/8f5c5ff19fa1c495cc82b608464b3bd7e23a2e27/lib/packaging/msi.rb#L14-L63
        foreach (var msiToSign in BuildParameters.GetMsisToSign())
        {
            if (BuildSystem.IsRunningOnTeamCity)
            {
                Information("Signing '{0}' using Certificate Subject Name {1}", msiToSign, BuildParameters.CertificateSubjectName);

                Sign(msiToSign, new SignToolSignSettings {
                        TimeStampUri = new Uri(BuildParameters.CertificateTimestampUrl),
                        UseMachineStore = true,
                        CertSubjectName = BuildParameters.CertificateSubjectName,
                        DigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true),
                        TimeStampDigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true)
                    });
            }
            else
            {
                Information("Signing '{0}' with {1}", msiToSign, BuildParameters.CertificateFilePath);

                var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

                Sign(msiToSign, new SignToolSignSettings {
                        TimeStampUri = new Uri(BuildParameters.CertificateTimestampUrl),
                        CertPath = BuildParameters.CertificateFilePath,
                        Password = password,
                        DigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true),
                        TimeStampDigestAlgorithm = (SignToolDigestAlgorithm)Enum.Parse(typeof(SignToolDigestAlgorithm), BuildParameters.CertificateAlgorithm, true)
                    });
            }

            if (FileExists(msiToSign))
            {
                BuildParameters.BuildProvider.UploadArtifact(msiToSign);
            }
        }
    }
    else
    {
        Information("There are no msi's defined to be signed.");
    }
});
