BuildParameters.Tasks.SignPowerShellScriptsTask = Task("Sign-PowerShellScripts")
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

        var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

        Information("Signing '{0}' with {1}", string.Join(",", BuildParameters.GetScriptsToSign()), BuildParameters.CertificateFilePath);

        var scriptsToSign = new List<string>();
        foreach (var filePath in BuildParameters.GetScriptsToSign())
        {
            scriptsToSign.Add(MakeAbsolute(filePath).FullPath);
        }

        StartPowershellFile(MakeAbsolute(powerShellSigningScript), args =>
            {
                args.AppendArray("ScriptsToSign", scriptsToSign)
                    .Append("TimeStampServer", BuildParameters.CertificateTimestampUrl)
                    .Append("CertificatePath", BuildParameters.CertificateFilePath)
                    .AppendSecret("CertificatePassword", password)
                    .Append("CertificateAlgorithm", BuildParameters.CertificateAlgorithm);
            });
    }
    else
    {
        Information("There are no PowerShell Scripts defined to be signed.");
    }
});

BuildParameters.Tasks.SignAssembliesTask = Task("Sign-Assemblies")
    .WithCriteria(() => (!string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath)) || BuildSystem.IsRunningOnTeamCity, "Skipping because unable to find certificate, and not running on TeamCity")
    .WithCriteria(() => BuildParameters.ShouldAuthenticodeSignOutputAssemblies, "Skipping since authenticode signing of output assemblies has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetFilesToSign != null)
    {
        var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

        foreach (var fileToSign in BuildParameters.GetFilesToSign())
        {
            if (BuildSystem.IsRunningOnTeamCity)
            {
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
    .WithCriteria(() => (!string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath)) || BuildSystem.IsRunningOnTeamCity, "Skipping because unable to find certificate, and not running on TeamCity")
    .WithCriteria(() => BuildParameters.ShouldAuthenticodeSignMsis, "Skipping since authenticode signing of msi's has been disabled")
    .Does(() =>
{
    if (BuildParameters.GetMsisToSign != null)
    {
        // dual signing Sha1 and Sha256 for an MSI would require https://github.com/puppetlabs/packaging/blob/8f5c5ff19fa1c495cc82b608464b3bd7e23a2e27/lib/packaging/msi.rb#L14-L63
        var password = System.IO.File.ReadAllText(BuildParameters.CertificatePassword);

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

                Sign(msiToSign, new SignToolSignSettings {
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
        Information("There are no msi's defined to be signed.");
    }
});