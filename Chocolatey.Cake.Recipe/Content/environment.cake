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

public static class Environment
{
    public static string DefaultPushSourceUrlVariable { get; private set; }
    public static string DependencyCheckDbConnectionStringVariable { get; private set; }
    public static string DependencyCheckDbDriverNameVariable { get; private set; }
    public static string DependencyCheckDbPasswordVariable { get; private set; }
    public static string DependencyCheckDbUserVariable { get; private set; }
    public static string DependencyCheckNvdApiKeyVariable { get; private set; }
    public static string DiscordWebHookUrlVariable { get; private set; }
    public static string DiscordUserNameVariable { get; private set; }
    public static string DiscordAvatarUrlVariable { get; private set; }
    public static string GitReleaseManagerTokenVariable { get; private set; }
    public static string MastodonHostNameVariable { get; private set; }
    public static string MastodonTokenVariable { get; private set; }
    public static string SlackChannelVariable { get; private set; }
    public static string SlackWebHookUrlVariable { get; private set; }
    public static string TransifexApiTokenVariable { get; private set; }
    public static string TwitterConsumerKeyVariable { get; private set; }
    public static string TwitterConsumerSecretVariable { get; private set; }
    public static string TwitterAccessTokenVariable { get; private set; }
    public static string TwitterAccessTokenSecretVariable { get; private set; }
    public static string SonarQubeTokenVariable { get; private set; }
    public static string SonarQubeIdVariable { get; private set; }
    public static string SonarQubeUrlVariable { get; private set; }
    public static string DockerUserVariable { get; private set; }
    public static string DockerPasswordVariable { get; private set; }
    public static string DockerServerVariable { get; private set; }

    public static void SetVariableNames(
        string defaultPushSourceUrlVariable = null,
        string dependencyCheckDbConnectionStringVariable = null,
        string dependencyCheckDbDriverNameVariable = null,
        string dependencyCheckDbPasswordVariable = null,
        string dependencyCheckDbUserVariable = null,
        string dependencyCheckNvdApiKeyVariable = null,
        string discordWebHookUrlVariable = null,
        string discordUserNameVariable = null,
        string discordAvatarUrlVariable = null,
        string gitReleaseManagerTokenVariable = null,
        string mastodonHostNameVariable = null,
        string mastodonTokenVariable = null,
        string slackChannelVariable = null,
        string slackWebHookUrlVariable = null,
        string transifexApiTokenVariable = null,
        string twitterConsumerKeyVariable = null,
        string twitterConsumerSecretVariable = null,
        string twitterAccessTokenVariable = null,
        string twitterAccessTokenSecretVariable = null,
        string sonarQubeTokenVariable = null,
        string sonarQubeIdVariable = null,
        string sonarQubeUrlVariable = null,
        string dockerUserVariable = null,
        string dockerPasswordVariable = null,
        string dockerServerVariable = null)
    {
        DefaultPushSourceUrlVariable = defaultPushSourceUrlVariable ?? "NUGETDEVPUSH_SOURCE";
        DependencyCheckDbConnectionStringVariable = dependencyCheckDbConnectionStringVariable ?? "DEPENDENCYCHECK_DB_CONNECTIONSTRING";
        DependencyCheckDbDriverNameVariable = dependencyCheckDbDriverNameVariable ?? "DEPENDENCYCHECK_DB_DRIVERNAME";
        DependencyCheckDbPasswordVariable = dependencyCheckDbPasswordVariable ?? "DEPENDENCYCHECK_DB_PASSWORD";
        DependencyCheckDbUserVariable = dependencyCheckDbUserVariable ?? "DEPENDENCYCHECK_DB_USER";
        DependencyCheckNvdApiKeyVariable = dependencyCheckNvdApiKeyVariable ?? "DEPENDENCYCHECK_NVD_API_KEY";
        DiscordWebHookUrlVariable = discordWebHookUrlVariable ?? "DISCORD_WEBHOOKURL";
        DiscordUserNameVariable = discordUserNameVariable ?? "DISCORD_USERNAME";
        DiscordAvatarUrlVariable = discordAvatarUrlVariable ?? "DISCORD_AVATARURL";
        GitReleaseManagerTokenVariable = gitReleaseManagerTokenVariable ?? "GITRELEASEMANAGER_PAT";
        MastodonHostNameVariable = mastodonHostNameVariable ?? "MASTODON_HOSTNAME";
        MastodonTokenVariable = mastodonTokenVariable ?? "MASTODON_TOKEN";
        SlackChannelVariable = slackChannelVariable ?? "SLACK_CHANNEL";
        SlackWebHookUrlVariable = slackWebHookUrlVariable ?? "SLACK_WEBHOOKURL";
        TransifexApiTokenVariable = transifexApiTokenVariable ?? "TRANSIFEX_API_TOKEN";
        TwitterConsumerKeyVariable = twitterConsumerKeyVariable ?? "TWITTER_CONSUMER_KEY";
        TwitterConsumerSecretVariable = twitterConsumerSecretVariable ?? "TWITTER_CONSUMER_SECRET";
        TwitterAccessTokenVariable = twitterAccessTokenVariable ?? "TWITTER_ACCESS_TOKEN";
        TwitterAccessTokenSecretVariable = twitterAccessTokenSecretVariable ?? "TWITTER_ACCESS_TOKEN_SECRET";
        SonarQubeTokenVariable = sonarQubeTokenVariable ?? "SONARQUBE_TOKEN";
        SonarQubeIdVariable = sonarQubeIdVariable ?? "SONARQUBE_ID";
        SonarQubeUrlVariable = sonarQubeUrlVariable ?? "SONARQUBE_URL";
        DockerUserVariable = dockerUserVariable ?? "DOCKER_USER";
        DockerPasswordVariable = dockerPasswordVariable ?? "DOCKER_PASSWORD";
        DockerServerVariable = dockerServerVariable ?? "DOCKER_SERVER";
    }
}