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

    public static DiscordCredentials FetchCredentials(ICakeContext context)
    {
        var webHookUrl = context.EnvironmentVariable(Environment.DiscordWebHookUrlVariable);

        if (string.IsNullOrEmpty(webHookUrl) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Web Hook URL for posting to Discord has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.DiscordWebHookUrlVariable);

            webHookUrl = context.Prompt("Enter Web Hook URL for posting to Discord:");
        }

        var userName = context.EnvironmentVariable(Environment.DiscordUserNameVariable);

        if (string.IsNullOrEmpty(userName) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required User Name for posting to Discord has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.DiscordUserNameVariable);

            userName = context.Prompt("Enter UserName for posting to Discord:");
        }

        var avatarUrl = context.EnvironmentVariable(Environment.DiscordAvatarUrlVariable);

        if (string.IsNullOrEmpty(avatarUrl) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Avatar URL for posting to Discord has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.DiscordAvatarUrlVariable);

            avatarUrl = context.Prompt("Enter Avatar URL for posting to Discord:");
        }

        return new DiscordCredentials(webHookUrl, userName, avatarUrl);
    }

    public DiscordCredentials(string webHookUrl, string userName, string avatarUrl)
    {
        WebHookUrl = webHookUrl;
        UserName = userName;
        AvatarUrl = avatarUrl;
    }
}

public class GitReleaseManagerCredentials
{
    public string Token { get; private set; }

    public static GitReleaseManagerCredentials FetchCredentials(ICakeContext context)
    {
        var token = context.EnvironmentVariable(Environment.GitReleaseManagerTokenVariable);

        if (string.IsNullOrEmpty(token) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Token for using GitReleaseManager has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.GitReleaseManagerTokenVariable);

            token = context.Prompt("Enter Token for GitReleaseManager:");
        }

        return new GitReleaseManagerCredentials(token);
    }

    public GitReleaseManagerCredentials(string token)
    {
        Token = token;
    }
}

public class MastodonCredentials
{
    public string Token { get; private set; }
    public string HostName { get; private set; }

    public static MastodonCredentials FetchCredentials(ICakeContext context)
    {
        var token = context.EnvironmentVariable(Environment.MastodonTokenVariable);

        if (string.IsNullOrEmpty(token) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Token for posting to Mastodon has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.MastodonTokenVariable);

            token = context.Prompt("Enter Token for Mastodon:");
        }

        var hostName = context.EnvironmentVariable(Environment.MastodonHostNameVariable);

        if (string.IsNullOrEmpty(hostName) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required HostName for posting to Mastodon has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.MastodonHostNameVariable);

            hostName = context.Prompt("Enter HostName for Mastodon:");
        }

        return new MastodonCredentials(token, hostName);
    }

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

    public static SlackCredentials FetchCredentials(ICakeContext context)
    {
        var channel = context.EnvironmentVariable(Environment.SlackChannelVariable);

        if (string.IsNullOrEmpty(channel) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Channel for posting to Slack has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.SlackChannelVariable);

            channel = context.Prompt("Enter Channel for Slack:");
        }

        var webHookUrl = context.EnvironmentVariable(Environment.SlackWebHookUrlVariable);

        if (string.IsNullOrEmpty(webHookUrl) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Channel for posting to Slack has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.SlackWebHookUrlVariable);

            webHookUrl = context.Prompt("Enter Web Hook URL for Slack:");
        }

        return new SlackCredentials(channel, webHookUrl);
    }

    public SlackCredentials(string channel, string webHookUrl)
    {
        Channel = channel;
        WebHookUrl = webHookUrl;
    }
}

public class TransifexCredentials
{
    public string ApiToken { get; private set; }

    public static TransifexCredentials FetchCredentials(ICakeContext context)
    {
        var apiToken = context.EnvironmentVariable(Environment.TransifexApiTokenVariable);

        if (string.IsNullOrEmpty(apiToken) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required API Token for using Transifex has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.TransifexApiTokenVariable);

            apiToken = context.Prompt("Enter API Token for Transifex:");
        }

        return new TransifexCredentials(apiToken);
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

    public static TwitterCredentials FetchCredentials(ICakeContext context)
    {
        var consumerKey = context.EnvironmentVariable(Environment.TwitterConsumerKeyVariable);

        if (string.IsNullOrEmpty(consumerKey) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Consumer Key for posting to Twitter has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.TwitterConsumerKeyVariable);

            consumerKey = context.Prompt("Enter Consumer Key for Twitter:");
        }

        var consumerSecret = context.EnvironmentVariable(Environment.TwitterConsumerSecretVariable);

        if (string.IsNullOrEmpty(consumerSecret) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Consumer Secret for posting to Twitter has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.TwitterConsumerSecretVariable);

            consumerSecret = context.Prompt("Enter Consumer Secret for Twitter:");
        }

        var accessToken = context.EnvironmentVariable(Environment.TwitterAccessTokenVariable);

        if (string.IsNullOrEmpty(accessToken) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Access Token for posting to Twitter has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.TwitterAccessTokenVariable);

            accessToken = context.Prompt("Enter Access Token for Twitter:");
        }

        var accessTokenSecret = context.EnvironmentVariable(Environment.TwitterAccessTokenSecretVariable);

        if (string.IsNullOrEmpty(accessTokenSecret) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Access Token Secret for posting to Twitter has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.TwitterAccessTokenSecretVariable);

            accessTokenSecret = context.Prompt("Enter Access Token Secret for Twitter:");
        }

        return new TwitterCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
    }

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

    public static SonarQubeCredentials FetchCredentials(ICakeContext context)
    {
        var token = context.EnvironmentVariable(Environment.SonarQubeTokenVariable);

        if (string.IsNullOrEmpty(token) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Token for using SonarQube has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.SonarQubeTokenVariable);

            token = context.Prompt("Enter Token for SonarQube:");
        }
        return new SonarQubeCredentials(token);
    }

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

    public static DockerCredentials FetchCredentials(ICakeContext context)
    {
        var user = context.EnvironmentVariable(Environment.DockerUserVariable);

        if (string.IsNullOrEmpty(user) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required User for using Docker has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.DockerUserVariable);

            user = context.Prompt("Enter User for Docker:");
        }

        var password = context.EnvironmentVariable(Environment.DockerPasswordVariable);

        if (string.IsNullOrEmpty(password) && BuildParameters.IsLocalBuild)
        {
            context.Warning("The required Password for using Docker has not been provided.");
            context.Warning("In future, this can be set using the {0} environment variable.", Environment.DockerPasswordVariable);

            password = context.Prompt("Enter Password for Docker:");
        }

        // This is not a required value
        var server = context.EnvironmentVariable(Environment.DockerServerVariable);

        return new DockerCredentials(user, password, server);
    }

    public DockerCredentials(string user, string password, string server = null)
    {
        Server = server;
        User = user;
        Password = password;
    }
}