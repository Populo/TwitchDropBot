using Discord;
using Discord.Rest;
using Microsoft.Extensions.Logging;
using RestSharp;
using TwitchDropBot.Data;
using TwitchDropBot.Service.Models;

namespace TwitchDropBot.Service;

public interface IDropService
{
    Task<ICollection<Drop>> GetDrops();
    bool HasSeenCampaign(Rewards drop);
    Task PostDrop(ulong channelId, Drop drop);
}

public class DropService(ILogger<DropService> logger, DiscordRestClient discordRestClient)
    : IDropService
{
    private readonly RestClient _client = new("https://twitch-drops-api.sunkwi.com/");
    private readonly ILogger<DropService> _logger = logger;

    public async Task<ICollection<Drop>> GetDrops()
    {
        var req = new RestRequest("/drops");
        try
        {
            var resp = await _client.GetAsync<Drop[]>(req);
            return resp;
        }
        catch (Exception e)
        {
            return new List<Drop>();
        }
    }

    public bool HasSeenCampaign(Rewards drop)
    {
        using var db = new DropContext();
        return db.Drops.Any(d => d.CampaignName == drop.name);
    }

    public async Task PostDrop(ulong channelId, Drop drop)
    {
        var channel = await discordRestClient.GetChannelAsync(channelId) as ITextChannel
            ?? throw new Exception("Channel not found");

        await using var db = new DropContext();
        
        List<Embed> embeds = [];
        var infoEmbed = CreateAnnounceEmbed(drop).Build();
        embeds.Add(infoEmbed);
        foreach (var campaign in drop.rewards)
        {
            if (HasSeenCampaign(campaign)) continue;
            
            db.Drops.Add(new DropDto()
            {
                Id = campaign.id,
                CampaignName = campaign.name,
                GameId = drop.gameId,
                EndAt = DateTime.Parse(campaign.endAt),
                StartAt = DateTime.Parse(campaign.startAt)
            });
            embeds.Add(CreateDropEmbed(campaign).Build());
        }

        // no unposted campaigns? do nothing
        if (embeds.Count == 1) return;
        
        await db.SaveChangesAsync();
        _logger.LogInformation("Posting {game} drops", drop.gameDisplayName);
        await channel.SendMessageAsync(embeds: embeds.ToArray());
    }

    private EmbedBuilder CreateAnnounceEmbed(Drop drop)
    {
        var embed =  new EmbedBuilder()
            .WithColor(Color.Parse("6441a5"))
            .WithThumbnailUrl(drop.gameBoxArtURL)
            .WithTitle("New Drops!")
            .AddField("Game", drop.gameDisplayName)
            .AddField("Start", $"<t:{DateTimeOffset.Parse(DateTime.Parse(drop.startAt).ToLongDateString()).ToUnixTimeSeconds()}:R>")
            .AddField("End", $"<t:{DateTimeOffset.Parse(DateTime.Parse(drop.endAt).ToLongDateString()).ToUnixTimeSeconds()}:R>");

        var campaigns = drop.rewards.Aggregate("", (current, r) => current + $"{r.name}\n"); 
        embed.AddField("Campaigns", campaigns.Trim());

        return embed;
    }

    private EmbedBuilder CreateDropEmbed(Rewards campaign)
    {
        var embed = new EmbedBuilder()
            .WithTitle(campaign.name)
            .WithColor(Color.Parse("6441a5"))
            .WithThumbnailUrl(campaign.imageURL)
            .WithUrl(campaign.detailsURL);

        foreach (var r in campaign.timeBasedDrops)
        {
            embed.AddField(r.name, $"{r.requiredMinutesWatched} minutes");
        }

        return embed;
    }
}