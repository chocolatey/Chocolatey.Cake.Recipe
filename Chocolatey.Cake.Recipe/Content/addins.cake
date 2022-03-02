///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin nuget:?package=Cake.Coverlet&version=2.5.4
#addin nuget:?package=Cake.Eazfuscator.Net&version=0.1.0
#addin nuget:?package=Cake.Figlet&version=1.2.0
#addin nuget:?package=Cake.FileHelpers&version=3.2.0
#addin nuget:?package=Cake.Git&version=0.19.0
#addin nuget:?package=Cake.Gulp&version=0.11.0
#addin nuget:?package=Cake.Incubator&version=5.1.0
#addin nuget:?package=Cake.Issues&version=0.7.1
#addin nuget:?package=Cake.Issues.MsBuild&version=0.7.2
#addin nuget:?package=Cake.Issues.InspectCode&version=0.7.1
#addin nuget:?package=Cake.Issues.Reporting&version=0.7.0
#addin nuget:?package=Cake.Issues.Reporting.Generic&version=0.7.2
#addin nuget:?package=Cake.Json&version=4.0.0
#addin nuget:?package=Cake.Kudu&version=0.8.0
#addin nuget:?package=Cake.Npm&version=0.16.0
#addin nuget:?package=Cake.PowerShell&version=0.4.8
#addin nuget:?package=Cake.ReSharperReports&version=0.10.0
#addin nuget:?package=Cake.StrongNameSigner&version=0.1.0
#addin nuget:?package=Cake.StrongNameTool&version=0.0.5
#addin nuget:?package=Cake.Transifex&version=1.0.1
#addin nuget:?package=MagicChunks&version=2.0.0.119

// TODO: Conditionally decide whether to install packages or not
#addin nuget:?package=Cake.Issues.PullRequests&version=0.7.0
#addin nuget:?package=Cake.Issues.PullRequests.AppVeyor&version=0.7.0

Action<string, IDictionary<string, string>> RequireAddin = (code, envVars) => {
    var script = MakeAbsolute(File(string.Format("./{0}.cake", Guid.NewGuid())));
    try
    {
        System.IO.File.WriteAllText(script.FullPath, code);
        var arguments = new Dictionary<string, string>();

        if (BuildParameters.CakeConfiguration.GetValue("NuGet_UseInProcessClient") != null) {
            arguments.Add("nuget_useinprocessclient", BuildParameters.CakeConfiguration.GetValue("NuGet_UseInProcessClient"));
        }

        if (BuildParameters.CakeConfiguration.GetValue("Settings_SkipVerification") != null) {
            arguments.Add("settings_skipverification", BuildParameters.CakeConfiguration.GetValue("Settings_SkipVerification"));
        }

        CakeExecuteScript(script,
            new CakeSettings
            {
                EnvironmentVariables = envVars,
                Arguments = arguments
            });
    }
    finally
    {
        if (FileExists(script))
        {
            DeleteFile(script);
        }
    }
};
