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
    if (FileExists("./.notifications/discord.txt"))
    {
        if (BuildParameters.CanPostToDiscord && BuildParameters.ShouldPostToDiscord)
        {
            var formattedMessage = System.IO.File.ReadAllText("./.notifications/discord.txt");
            var messageArguments = BuildParameters.DiscordMessageArguments(BuildParameters.Version);
            
            //Information(formattedMessage, messageArguments);
            SendMessageToDiscord(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Discord message. CanPostToDiscord: {0}, ShouldPostToDiscord: {1}", BuildParameters.CanPostToDiscord, BuildParameters.ShouldPostToDiscord);
        }
    }
    else
    {
        Information("Skipping sending notiifation to Discord, since input template file does not exist.");
    }

    if (FileExists("./.notifications/mastodon.txt"))
    {
        if (BuildParameters.CanPostToMastodon && BuildParameters.ShouldPostToMastodon)
        {
            var formattedMessage = System.IO.File.ReadAllText("./.notifications/mastodon.txt");
            var messageArguments = BuildParameters.MastodonMessageArguments(BuildParameters.Version);

            //Information(formattedMessage, messageArguments);
            SendMessageToMastodon(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Mastodon message. CanPostToMastodon: {0}, ShouldPostToMastodon: {1}", BuildParameters.CanPostToMastodon, BuildParameters.ShouldPostToMastodon);
        }
    }
    else
    {
        Information("Skipping sending notiifation to Mastodon, since input template file does not exist.");
    }

    if (FileExists("./.notifications/slack.txt"))
    {
        if (BuildParameters.CanPostToSlack && BuildParameters.ShouldPostToSlack)
        {
            var formattedMessage = System.IO.File.ReadAllText("./.notifications/slack.txt");
            var messageArguments = BuildParameters.SlackMessageArguments(BuildParameters.Version);

            //Information(formattedMessage, messageArguments);
            SendMessageToSlackChannel(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Slack message. CanPostToSlack: {0}, ShouldPostToSlack: {1}", BuildParameters.CanPostToSlack, BuildParameters.ShouldPostToSlack);
        }
    }
    else
    {
        Information("Skipping sending notiifation to Slack, since input template file does not exist.");
    }

    if (FileExists("./.notifications/twitter.txt"))
    {
        if (BuildParameters.CanPostToTwitter && BuildParameters.ShouldPostToTwitter)
        {
            var formattedMessage = System.IO.File.ReadAllText("./.notifications/twitter.txt");
            var messageArguments = BuildParameters.TwitterMessageArguments(BuildParameters.Version);

            Information(formattedMessage, messageArguments);
            //SendMessageToTwitter(string.Format(formattedMessage, messageArguments));
        }
        else
        {
            Warning("Unable to send Twitter message. CanPostToTwitter: {0}, ShouldPostToTwitter: {1}", BuildParameters.CanPostToTwitter, BuildParameters.ShouldPostToTwitter);
        }
    }
    else
    {
        Information("Skipping sending notiifation to Twitter, since input template file does not exist.");
    }
});