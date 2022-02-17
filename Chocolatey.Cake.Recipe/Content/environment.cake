public static class Environment
{
    public static string DefaultPushSourceUrlVariable { get; private set; }

    public static void SetVariableNames(
        string defaultPushSourceUrlVariable = null)
    {
        DefaultPushSourceUrlVariable = defaultPushSourceUrlVariable ?? "NUGETDEV_SOURCE";
    }
}