using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using TwitchDropBot.Data;
using TwitchDropBot.Service;

namespace TwitchDropBot.Bot.Commands;

public partial class Commands(IBotService botService, IDropService dropService)
{
    #region Builders
    
    private static readonly SlashCommandBuilder IgnoreBuilder = new()
    {
        Name = "ignore",
        Description = "Ignore or allow game drop notifications",
        DefaultMemberPermissions = GuildPermission.ManageGuild,
        Options =
        [
            new SlashCommandOptionBuilder
            {
                Name = "game",
                Description = "The name of the game to ignore",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String
            },

            new SlashCommandOptionBuilder
            {
                Name = "ignore",
                Description = "Whether to ignore or unignore the game",
                IsRequired = true,
                Type = ApplicationCommandOptionType.Boolean
            }
        ]
    };

    private static readonly SlashCommandBuilder CheckDropsBuilder = new()
    {
        Name = "check-twitch",
        Description = "Check for new drops",
        DefaultMemberPermissions = GuildPermission.ManageGuild
    };

    private static readonly SlashCommandBuilder ListGamesBuilder = new()
    {
        Name = "list-games",
        Description = "List games",
        Options =
        [
            new SlashCommandOptionBuilder
            {
                Name = "filter",
                Description = "game list filter",
                IsRequired = true,
                Type = ApplicationCommandOptionType.Integer,
                Choices = [
                    new ApplicationCommandOptionChoiceProperties()
                    {
                        Name = "ignored",
                        Value = (int)IgnoredGamesFilter.Ignored
                    },
                    new ApplicationCommandOptionChoiceProperties()
                    {
                        Name = "allowed",
                        Value = (int)IgnoredGamesFilter.Allowed
                    },
                    new ApplicationCommandOptionChoiceProperties()
                    {
                        Name = "all",
                        Value = (int)IgnoredGamesFilter.All
                    }
                ]
            }
        ]
    };

    private static readonly SlashCommandBuilder GetCampaignsBuilder = new()
    {
        Name = "get-drops",
        Description = "Get all drops for a game",
        Options =
        [
            new SlashCommandOptionBuilder
            {
                Name = "game",
                Description = "The name of the game to get drops (theres too many to allow autocomplete sorry)",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String
            }
        ]
    };

    public static readonly List<SlashCommandBuilder> CommandBuilders =
        [IgnoreBuilder, ListGamesBuilder, CheckDropsBuilder, GetCampaignsBuilder];

    #endregion
    
    #region Command
    
    public async Task IgnoreGame(SocketSlashCommand arg, DiscordSocketClient client)
    {
        await arg.DeferAsync(ephemeral: true);
        if (!botService.IsAdmin(arg.User.Id))
        {
            await arg.FollowupAsync("You are not allowed to use this command", ephemeral: true);
            await botService.SendToErrorAsync($"User {arg.User.Username} tried to use Ignore command");
            
            return;
        }
        
        var gameName = arg.Data.Options.First(o => o.Name == "game").Value.ToString();
        var ignore = (bool)arg.Data.Options.First(o => o.Name == "ignore").Value;
        gameName = CleanString(gameName!);
        
        await using var db = new DropContext();
        var games = db.Games.ToList();
        var game = games.FirstOrDefault(g =>
        {
            var cleanedName = CleanString(g.Name);
            return cleanedName == gameName;
        });

        if (null == game)
        {
            await arg.FollowupAsync("Game not found", ephemeral: true);
            return;
        }

        var dbGame = db.Games.FirstOrDefault(g => g.Id == game.Id);
        
        dbGame!.Ignored = ignore;
        if (!ignore)
        {
            // remove games to post after unignoring
            db.Drops.RemoveRange(db.Drops.Where(d => d.Game.Id == dbGame.Id));
        }
        
        await db.SaveChangesAsync();
        
        await arg.FollowupAsync($"Game {game.Name} {(ignore ? "ignored" : "allowed")}", ephemeral: true);
    }

    public async Task ListGames(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);
        var filterValue = (long)arg.Data.Options.First(o => o.Name == "filter").Value!;
        var filter = (IgnoredGamesFilter)filterValue; // comes in as a long for some reason
        
        await using var db = new DropContext();
        var games = db.Games.AsQueryable();
        if (filter != IgnoredGamesFilter.All)
        {
            games = games.Where(g => g.Ignored == (filter == IgnoredGamesFilter.Ignored));
        }
        
        var message = games.Any()
            ? string.Join("\n", games.OrderBy(g => g.Name).Select(g => $"`{g.Name}`"))
            : "No games found.";
        
        await arg.FollowupAsync(message, ephemeral: true);
    }

    public async Task GetDrops(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);
        var game = arg.Data.Options.First(o => o.Name == "game").Value.ToString();
        game = CleanString(game!);
        
        var drop = (await dropService.GetDrops())
            .FirstOrDefault(d =>
            {
                var cleanedName = CleanString(d.gameDisplayName);
                return cleanedName.Contains(game.ToLower());
            });

        if (null == drop)
        {
            await arg.FollowupAsync("No drops found", ephemeral: true);
            return;
        }

        List<Embed> embeds =
        [
            dropService.CreateAnnounceEmbed(drop, ignoreSeen: false).Build()
        ];
        
        embeds.AddRange(
            from c
            in drop.rewards
            where c.status.Equals("ACTIVE", StringComparison.InvariantCultureIgnoreCase) 
            select dropService.CreateDropEmbed(c).Build()
        );
        
        await arg.FollowupAsync(embeds: embeds.ToArray(), ephemeral: true);
    }
    
    #endregion
    
    #region Helpers
    
    [GeneratedRegex("[^a-z0-9 ]")]
    private static partial Regex GameNameFilter();
    
    private static string CleanString(string input) => GameNameFilter()
        .Replace(input
            .ToLower() ?? "", "")
        .Trim();
    
    #endregion

}

public enum IgnoredGamesFilter
{
    All = 0,
    Allowed = 1,
    Ignored = 2
}