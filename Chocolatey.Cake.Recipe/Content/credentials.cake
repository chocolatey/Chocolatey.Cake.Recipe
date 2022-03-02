public class GitHubCredentials
{
    public string Token { get; private set; }

    public GitHubCredentials(string token)
    {
        Token = token;
    }
}

public class PackageSourceCredentials
{
    public string ApiKey { get; private set; }
    public string User { get; private set; }
    public string Password { get; private set; }

    public PackageSourceCredentials(string apiKey, string user, string password)
    {
        ApiKey = apiKey;
        User = user;
        Password = password;
    }
}

public static GitHubCredentials GetGitHubCredentials(ICakeContext context)
{
    string token = null;
    // if "GitHubTokenVariable" is not set, fallback to the gh-cli defaults of GH_TOKEN, GITHUB_TOKEN
    var variableNames = new[]{ Environment.GitHubTokenVariable, "GH_TOKEN", "GITHUB_TOKEN" };
    foreach (var name in variableNames)
    {
        token = context.EnvironmentVariable(name);
        if (!string.IsNullOrEmpty(token))
        {
            break;
        }
    }

    return new GitHubCredentials(token);
}