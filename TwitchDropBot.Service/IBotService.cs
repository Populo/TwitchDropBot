using Discord;
using Discord.Rest;

namespace TwitchDropBot.Service;

public interface IBotService
{
    Task SendToPostAsync(string? message = null, Embed[]? embeds = null, bool crosspost = false);
    Task SendToErrorAsync(string? message = null, Embed[]? embeds = null);
    bool IsAdmin(ulong userId);
}

public class BotService(DiscordRestClient discordRestClient, List<string> notifyChannels, List<string> errorChannels, List<string> botAdmins) : IBotService
{
    public async Task SendToPostAsync(string? message, Embed[]? embeds = null, bool crosspost = false)
    {
        foreach (var channelId in notifyChannels)
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

    public async Task SendToErrorAsync(string? message, Embed[]? embeds = null)
    {
        foreach (var cId in errorChannels)
        {
            var channel = await discordRestClient.GetChannelAsync(ulong.Parse(cId)) as ITextChannel
                          ?? throw new Exception("Channel not found");
            await channel.SendMessageAsync(message, embeds: embeds);
        }
    }

    public bool IsAdmin(ulong userId)
    {
        return botAdmins.Contains(userId.ToString());
    }
}