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
    Task PostDrop(Drop drop);
    EmbedBuilder CreateAnnounceEmbed(Drop drop, bool ignoreSeen = true);
    EmbedBuilder CreateDropEmbed(Rewards campaign);
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

    private bool HasSeenCampaign(Rewards drop)
    {
        using var db = new DropContext();
        return db.Drops.Any(d => d.CampaignName == drop.name);
    }

    public async Task PostDrop(Drop drop)
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

                await botService.SendToErrorAsync("New game detected: " + game.Name + ". Auto ignored.");
                
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

            // dont post if campaign is over
            if (campaign.status.Equals("ACTIVE", StringComparison.InvariantCultureIgnoreCase))
            {
                embeds.Add(CreateDropEmbed(campaign).Build());
            }
            else
            {
                await botService.SendToErrorAsync(message: $"status not active.\n- game: {drop.gameDisplayName}\n- status: {campaign.status}", 
                    embeds: [CreateDropEmbed(campaign).Build()]);
            }
        }

        // no unposted campaigns? do nothing
        if (embeds.Count == 1) return;
        
        await db.SaveChangesAsync();
        logger.LogInformation("Posting {game} drops", drop.gameDisplayName);
        await botService.SendToPostAsync(embeds: embeds.ToArray(), crosspost: true);
    }

    public EmbedBuilder CreateAnnounceEmbed(Drop drop, bool ignoreSeen = true)
    {
        var embed = new EmbedBuilder()
            .WithColor(Color.Parse("6441a5"))
            .WithThumbnailUrl(drop.gameBoxArtURL)
            .WithTitle("New Drops!")
            .AddField("Game", drop.gameDisplayName);

        var campaigns = drop.rewards.AsEnumerable();
        if (ignoreSeen) campaigns = campaigns.Where(c => !HasSeenCampaign(c));
        
        var rewardsEnumerable = campaigns as Rewards[] ?? campaigns.ToArray();
        var campaignText = rewardsEnumerable.Length != 0
            ? rewardsEnumerable.Aggregate("", (current, r) => current + $"{r.name}\n") 
            : "c"; 
        
        embed.AddField("Campaigns", campaignText);

        return embed;
    }

    public EmbedBuilder CreateDropEmbed(Rewards campaign)
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
            var isGift = r.requiredSubs > 0;
            var isTime = r.requiredMinutesWatched > 0;
            
            var fieldText = "";
            if (isTime)
            {
                fieldText = $"{r.requiredMinutesWatched} minutes";
                if (isGift) fieldText += $" and {r.requiredSubs} subs";
            }
            else
            {
                fieldText = $"{r.requiredSubs} sub(s)";
            }
            
            embed.AddField(r.name, fieldText);
        }

        return embed;
    }
}