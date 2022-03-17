public static class Environment
{
    public static string DefaultPushSourceUrlVariable { get; private set; }
    public static string GitHubTokenVariable { get; private set; }
    public static string TransifexApiTokenVariable { get; private set; }

    public static void SetVariableNames(
        string defaultPushSourceUrlVariable = null,
        string gitHubTokenVariable = null,
        string transifexApiTokenVariable = null)
    {
        DefaultPushSourceUrlVariable = defaultPushSourceUrlVariable ?? "NUGETDEVPUSH_SOURCE";
        GitHubTokenVariable = gitHubTokenVariable ?? "GITHUB_PAT";
        TransifexApiTokenVariable = transifexApiTokenVariable ?? "TRANSIFEX_API_TOKEN";
    }
}