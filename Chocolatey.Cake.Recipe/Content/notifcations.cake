// Copyright © 2023 Chocolatey Software, Inc
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

public void SendMessageToDiscord(string message)
{
    try
    {
        Information("Sending message to Discord...");
        
        var postMessageResult = Discord.Chat.PostMessage(
            webHookUrl: BuildParameters.Discord.WebHookUrl,
            content: message,
            messageSettings: new DiscordChatMessageSettings {
                UserName = BuildParameters.Discord.UserName,
                AvatarUrl = new Uri(BuildParameters.Discord.AvatarUrl)
            }
        );

        if (postMessageResult.Ok)
        {
            Information("Discord message {0} successfully sent", postMessageResult.TimeStamp);
        }
        else
        {
            Error("Failed to send Discord message: {0}", postMessageResult.Error);
        }
    }
    catch (Exception ex)
    {
        Error("{0}", ex);
    }
}

public void SendMessageToMastodon(string message)
{
    try
    {
        Information("Sending message to Mastodon...");

        var result = MastodonSendToot(
            hostName: BuildParameters.Mastodon.HostName,
            accessToken: BuildParameters.Mastodon.Token, 
            text: message, 
            idempotencyKey: Guid.NewGuid().ToString());

        if (result.IsSuccess)
        {
            Information("Mastodon messsage successfully sent");
        }
        else
        {
            Error("Failed to send Mastodon message: {0}", result.ReasonPhrase);
        }
    }
    catch (Exception ex)
    {
        Error("{0}", ex);
    }
}

public void SendMessageToSlackChannel(string message)
{
    try
    {
        Information("Sending message to Slack...");

        var postMessageResult = Slack.Chat.PostMessage(
                    channel: BuildParameters.Slack.Channel,
                    text: message,
                    messageSettings: new SlackChatMessageSettings { IncomingWebHookUrl = BuildParameters.Slack.WebHookUrl }
            );

        if (postMessageResult.Ok)
        {
            Information("Slack Message {0} successfully sent", postMessageResult.TimeStamp);
        }
        else
        {
            Error("Failed to send Slack message: {0}", postMessageResult.Error);
        }
    }
    catch (Exception ex)
    {
        Error("{0}", ex);
    }
}

public void SendMessageToTwitter(string message)
{
    try
    {
        Information("Sending message to Twitter...");

        TwitterSendTweet(BuildParameters.Twitter.ConsumerKey,
                         BuildParameters.Twitter.ConsumerSecret,
                         BuildParameters.Twitter.AccessToken,
                         BuildParameters.Twitter.AccessTokenSecret,
                         message);

        Information("Twitter message successfully sent.");
    }
    catch (Exception ex)
    {
        Error("{0}", ex);
    }
}

BuildParameters.Tasks.SendNotificationsTask = Task("Send-Notifications")
    .Does(() => 
{
    bool dryRun = Context.Argument("dry-run", false);

    if (FileExists("./.notifications/discord.txt"))
    {
        var formattedMessage = System.IO.File.ReadAllText("./.notifications/discord.txt");
        var messageArguments = BuildParameters.DiscordMessageArguments(BuildParameters.Version);

        if (dryRun)
        {
            Warning("Would have sent the following to Discord:");
            Information(formattedMessage, messageArguments);
        }
        else if (BuildParameters.CanPostToDiscord && BuildParameters.ShouldPostToDiscord)
        {
            SendMessageToDiscord(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Discord message. CanPostToDiscord: {0}, ShouldPostToDiscord: {1}", BuildParameters.CanPostToDiscord, BuildParameters.ShouldPostToDiscord);
        }
    }
    else
    {
        Information("Skipping sending notification to Discord, since input template file does not exist.");
    }

    if (FileExists("./.notifications/mastodon.txt"))
    {
        var formattedMessage = System.IO.File.ReadAllText("./.notifications/mastodon.txt");
        var messageArguments = BuildParameters.MastodonMessageArguments(BuildParameters.Version);

        if (dryRun)
        {
            Warning("Would have sent the following to Mastodon:");
            Information(formattedMessage, messageArguments);
        }
        else if (BuildParameters.CanPostToMastodon && BuildParameters.ShouldPostToMastodon)
        {
            SendMessageToMastodon(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Mastodon message. CanPostToMastodon: {0}, ShouldPostToMastodon: {1}", BuildParameters.CanPostToMastodon, BuildParameters.ShouldPostToMastodon);
        }
    }
    else
    {
        Information("Skipping sending notification to Mastodon, since input template file does not exist.");
    }

    if (FileExists("./.notifications/slack.txt"))
    {
        var formattedMessage = System.IO.File.ReadAllText("./.notifications/slack.txt");
        var messageArguments = BuildParameters.SlackMessageArguments(BuildParameters.Version);

        if (dryRun)
        {
            Warning("Would have sent the following to Slack:");
            Information(formattedMessage, messageArguments);
        }
        else if (BuildParameters.CanPostToSlack && BuildParameters.ShouldPostToSlack)
        {
            SendMessageToSlackChannel(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Slack message. CanPostToSlack: {0}, ShouldPostToSlack: {1}", BuildParameters.CanPostToSlack, BuildParameters.ShouldPostToSlack);
        }
    }
    else
    {
        Information("Skipping sending notification to Slack, since input template file does not exist.");
    }

    if (FileExists("./.notifications/twitter.txt"))
    {
        var formattedMessage = System.IO.File.ReadAllText("./.notifications/twitter.txt");
        var messageArguments = BuildParameters.TwitterMessageArguments(BuildParameters.Version);

        if (dryRun)
        {
            Warning("Would have sent the following to Twitter:");
            Information(formattedMessage, messageArguments);
        }
        else if (BuildParameters.CanPostToTwitter && BuildParameters.ShouldPostToTwitter)
        {
            SendMessageToTwitter(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Twitter message. CanPostToTwitter: {0}, ShouldPostToTwitter: {1}", BuildParameters.CanPostToTwitter, BuildParameters.ShouldPostToTwitter);
        }
    }
    else
    {
        Information("Skipping sending notification to Twitter, since input template file does not exist.");
    }
});