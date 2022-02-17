BuildParameters.Tasks.SignPowerShellScriptsTask = Task("Sign-PowerShellScripts")
    .WithCriteria(() => !string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath), "Skipping because unable to find certificate")
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
});

BuildParameters.Tasks.SignAssembliesTask = Task("Sign-Assemblies")
    .WithCriteria(() => !string.IsNullOrWhiteSpace(BuildParameters.CertificateFilePath) && FileExists(BuildParameters.CertificateFilePath), "Skipping because unable to find certificate")
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
        Information("There are no files defined to be signed.");
    }
});