using Discord;
using Discord.Rest;

namespace TwitchDropBot.Service;

public interface IBotService
{
    Task SendToPostAsync(List<string> channels, string message, Embed[]? embeds = null, bool crosspost = true);
    Task SendToErrorAsync(List<string> channels, string message);
}

public class BotService(DiscordRestClient discordRestClient) : IBotService
{
    public async Task SendToPostAsync(List<string> channels, string message, Embed[]? embeds = null, bool crosspost = true)
    {
        foreach (var channelId in channels)
        {
            var channel = await discordRestClient.GetChannelAsync(ulong.Parse(channelId)) as ITextChannel
                          ?? throw new Exception("Channel not found");
            var sent = await channel.SendMessageAsync(message, embeds: embeds);
            
            if (!crosspost) continue;
            
            try
            {
                await sent.CrosspostAsync();
            }
            catch
            {
                // dont care if this fails
            } 

        }
    }

    public async Task SendToErrorAsync(List<string> channels, string message)
    {
        foreach (var cId in channels)
        {
            var channel = await discordRestClient.GetChannelAsync(ulong.Parse(cId)) as ITextChannel
                          ?? throw new Exception("Channel not found");
            await channel.SendMessageAsync(message);
        }
    }
}