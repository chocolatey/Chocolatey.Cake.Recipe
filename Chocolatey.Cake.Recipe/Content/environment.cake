public static class Environment
{
    public static string GitHubTokenVariable { get; private set; }
    public static string DefaultPushSourceUrlVariable { get; private set; }

    public static void SetVariableNames(
        string gitHubTokenVariable = null,
        string defaultPushSourceUrlVariable = null)
    {
        GitHubTokenVariable = gitHubTokenVariable ?? "GITHUB_PAT";
        DefaultPushSourceUrlVariable = defaultPushSourceUrlVariable ?? "NUGETDEV_SOURCE";
    }
}