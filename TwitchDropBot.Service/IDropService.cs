using Discord;
using Discord.Rest;
using Microsoft.Extensions.Logging;
using RestSharp;
using TwitchDropBot.Data;
using TwitchDropBot.Service.Models;
using Game = TwitchDropBot.Data.Game;

namespace TwitchDropBot.Service;

public interface IDropService
{
    Task<ICollection<Drop>> GetDrops();
    bool HasSeenCampaign(Rewards drop);
    Task PostDrop(List<string> channels, Drop drop);
    Task<Game> IgnoreGame(string gameName, bool ignore = true);
}

public class DropService(ILogger<DropService> logger, DiscordRestClient discordRestClient, IBotService botService)
    : IDropService
{
    private readonly RestClient _client = new("https://twitch-drops-api.sunkwi.com/");

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

    public async Task PostDrop(List<string> channels, Drop drop)
    {
        await using var db = new DropContext();
        
        List<Embed> embeds = [];
        var infoEmbed = CreateAnnounceEmbed(drop).Build();
        embeds.Add(infoEmbed);
        foreach (var campaign in drop.rewards)
        {
            var game = db.Games.FirstOrDefault(g => g.Id == drop.gameId);
            
            if (null == game)
            {
                game = new Game()
                {
                    Id = drop.gameId,
                    Name = drop.gameDisplayName,
                    Ignored = true
                };
                db.Games.Add(game);

                await botService.SendToPostAsync(channels, "New game detected: " + game.Name + ". Auto ignored.", crosspost: false);
                
                await db.SaveChangesAsync();
            }

            if (game.Name != drop.gameDisplayName)
            {
                game.Name = drop.gameDisplayName;
                await db.SaveChangesAsync();
            }
            
            if (HasSeenCampaign(campaign)) continue;
            
            db.Drops.Add(new DropDto()
            {
                Id = campaign.id,
                CampaignName = campaign.name,
                Game = game,
                EndAt = DateTime.Parse(campaign.endAt),
                StartAt = DateTime.Parse(campaign.startAt)
            });
            
            // add to db even if we arent posting because were ignoring it
            if (game.Ignored) continue;
            
            embeds.Add(CreateDropEmbed(campaign).Build());
        }

        // no unposted campaigns? do nothing
        if (embeds.Count == 1) return;
        
        await db.SaveChangesAsync();
        logger.LogInformation("Posting {game} drops", drop.gameDisplayName);
        foreach (var channelId in channels)
        {
            var channel = await discordRestClient.GetChannelAsync(ulong.Parse(channelId)) as ITextChannel
                ?? throw new Exception("Channel not found");
            var message = await channel.SendMessageAsync(embeds: embeds.ToArray());
            try
            {
                await message.CrosspostAsync();
            }
            catch {} // exception if non news channel, dont care
        }
    }

    private EmbedBuilder CreateAnnounceEmbed(Drop drop)
    {
        var embed = new EmbedBuilder()
            .WithColor(Color.Parse("6441a5"))
            .WithThumbnailUrl(drop.gameBoxArtURL)
            .WithTitle("New Drops!")
            .AddField("Game", drop.gameDisplayName);

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
            .WithUrl(campaign.detailsURL)
            .AddField("Start", $"<t:{DateTimeOffset.Parse(DateTime.Parse(campaign.startAt).ToLongDateString()).ToUnixTimeSeconds()}:R>")
            .AddField("End", $"<t:{DateTimeOffset.Parse(DateTime.Parse(campaign.endAt).ToLongDateString()).ToUnixTimeSeconds()}:R>");

        foreach (var r in campaign.timeBasedDrops)
        {
            embed.AddField(r.name, $"{r.requiredMinutesWatched} minutes");
        }

        return embed;
    }
    
    public async Task<Game> IgnoreGame(string gameId, bool ignore = true)
    {
        await using var db = new DropContext();
        var game = db.Games.FirstOrDefault(g => g.Id == gameId)
            ?? throw new Exception("Game not found");
        
        game.Ignored = ignore;
        await db.SaveChangesAsync();
        return game;
    }
}