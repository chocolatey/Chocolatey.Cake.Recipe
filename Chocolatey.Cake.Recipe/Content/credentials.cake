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

public class DiscordCredentials
{
    public string WebHookUrl { get; private set; }
    public string UserName { get; private set; }
    public string AvatarUrl { get; private set; }

    public DiscordCredentials(string webHookUrl, string userName, string avatarUrl)
    {
        WebHookUrl = webHookUrl;
        UserName = userName;
        AvatarUrl = avatarUrl;
    }
}

public class GitHubCredentials
{
    public string Token { get; private set; }

    public GitHubCredentials(string token)
    {
        Token = token;
    }
}

public class MastodonCredentials
{
    public string Token { get; private set; }
    public string HostName { get; private set; }

    public MastodonCredentials(string token, string hostName)
    {
        Token = token;
        HostName = hostName;
    }
}

public class SlackCredentials
{
    public string Channel { get; private set; }
    public string WebHookUrl { get; private set; }

    public SlackCredentials(string channel, string webHookUrl)
    {
        Channel = channel;
        WebHookUrl = webHookUrl;
    }
}

public class TransifexCredentials
{
    public string ApiToken { get; private set; }

    public bool HasCredentials
    {
        get { return !string.IsNullOrEmpty(ApiToken); }
    }

    public TransifexCredentials(string apiToken)
    {
        ApiToken = apiToken;
    }
}

public class TwitterCredentials
{
    public string ConsumerKey { get; private set; }
    public string ConsumerSecret { get; private set; }
    public string AccessToken { get; private set; }
    public string AccessTokenSecret { get; private set; }

    public TwitterCredentials(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
    {
        ConsumerKey = consumerKey;
        ConsumerSecret = consumerSecret;
        AccessToken = accessToken;
        AccessTokenSecret = accessTokenSecret;
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

public class SonarQubeCredentials
{
    public string Token { get; private set; }

    public SonarQubeCredentials(string token)
    {
        Token = token;
    }
}

public class DockerCredentials
{
    public string Server { get; private set; }
    public string User { get; private set; }
    public string Password { get; private set; }

    public bool HasCredentials
    {
        get { return !string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Password); }
    }

    public DockerCredentials(string user, string password, string server = null)
    {
        Server = server;
        User = user;
        Password = password;
    }
}

public static DiscordCredentials GetDiscordCredentials(ICakeContext context)
{
    return new DiscordCredentials(
        context.EnvironmentVariable(Environment.DiscordWebHookUrlVariable),
        context.EnvironmentVariable(Environment.DiscordUserNameVariable),
        context.EnvironmentVariable(Environment.DiscordAvatarUrlVariable)
    );
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

public static MastodonCredentials GetMastodonCredentials(ICakeContext context)
{
    return new MastodonCredentials(
        context.EnvironmentVariable(Environment.MastodonTokenVariable),
        context.EnvironmentVariable(Environment.MastodonHostNameVariable)
    );
}

public static SlackCredentials GetSlackCredentials(ICakeContext context)
{
    return new SlackCredentials(
        context.EnvironmentVariable(Environment.SlackChannelVariable),
        context.EnvironmentVariable(Environment.SlackWebHookUrlVariable)
    );
}

public static TransifexCredentials GetTransifexCredentials(ICakeContext context)
{
    return new TransifexCredentials(
        context.EnvironmentVariable(Environment.TransifexApiTokenVariable)
    );
}

public static TwitterCredentials GetTwitterCredentials(ICakeContext context)
{
    return new TwitterCredentials(
        context.EnvironmentVariable(Environment.TwitterConsumerKeyVariable),
        context.EnvironmentVariable(Environment.TwitterConsumerSecretVariable),
        context.EnvironmentVariable(Environment.TwitterAccessTokenVariable),
        context.EnvironmentVariable(Environment.TwitterAccessTokenSecretVariable)
    );
}

public static SonarQubeCredentials GetSonarQubeCredentials(ICakeContext context)
{
    return new SonarQubeCredentials(
        context.EnvironmentVariable(Environment.SonarQubeTokenVariable)
    );
}

public static DockerCredentials GetDockerCredentials(ICakeContext context)
{
    return new DockerCredentials(
        context.EnvironmentVariable(Environment.DockerUserVariable),
        context.EnvironmentVariable(Environment.DockerPasswordVariable),
        context.EnvironmentVariable(Environment.DockerServerVariable)
    );
}