using Discord;
using Discord.WebSocket;
using TwitchDropBot.Bot.Helpers;
using TwitchDropBot.Data;
using TwitchDropBot.Service;

namespace TwitchDropBot.Bot.Commands;

public class Commands(IBotService botService)
{
    #region Builders
    
    private static readonly SlashCommandBuilder IgnoreBuilder = new()
    {
        Name = "ignore",
        Description = "Ignore or unignore game drops from being sent to the channel",
        DefaultMemberPermissions = GuildPermission.ManageGuild,
        Options = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "game",
                Description = "The name of the game to ignore",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String
            },
            new()
            {
                Name = "ignore",
                Description = "Whether to ignore or unignore the game",
                IsRequired = true,
                Type = ApplicationCommandOptionType.Boolean
            }
        }
    };
    
    private static readonly SlashCommandBuilder IgnoredGamesBuilder = new()
    {
        Name = "ignored-games",
        Description = "Show a list of all ignored games",
    };
    
    private static readonly SlashCommandBuilder NonIgnoredGamesBuilder = new()
    {
        Name = "allowed-games",
        Description = "Show a list of all allowed games",
    };

    public static readonly List<SlashCommandBuilder> CommandBuilders =
        [IgnoreBuilder, IgnoredGamesBuilder, NonIgnoredGamesBuilder];

    #endregion
    
    #region Command
    
    public async Task IgnoreGame(SocketSlashCommand arg, DiscordSocketClient client)
    {
        await arg.DeferAsync(ephemeral: true);
        if (!IsAllowed(arg.User.Id))
        {
            await arg.FollowupAsync("You are not allowed to use this command", ephemeral: true);
            await botService.SendToErrorAsync(BotConfiguration.ErrorChannels,
                $"User {arg.User.Username} tried to use Ignore command");
            
            return;
        }
        
        var gameId = arg.Data.Options.First(o => o.Name == "game").Value.ToString();
        var ignore = (bool)arg.Data.Options.First(o => o.Name == "ignore").Value;
        
        await using var db = new DropContext();
        var game = db.Games.FirstOrDefault(g => g.Name == gameId);
        
        if (null == game) await arg.FollowupAsync("Game not found", ephemeral: true);
        
        game!.Ignored = ignore;
        await db.SaveChangesAsync();
        
        await arg.FollowupAsync($"Game {game.Name} {(ignore ? "ignored" : "allowed")}", ephemeral: true);
    }

    public async Task ListGames(SocketSlashCommand arg, bool ignored)
    {
        await arg.DeferAsync(ephemeral: true);
        
        await using var db = new DropContext();
        var games = db.Games.Where(g => g.Ignored == ignored).ToList();

        var message = games.Count != 0
            ? string.Join("\n", games.OrderBy(g => g.Name).Select(g => $"`{g.Name}`"))
            : "No games found.";
        
        await arg.FollowupAsync(message, ephemeral: true);
    }
    
    #endregion
    
    #region Helpers
    
    private static bool IsAllowed(ulong userId)
    {
        // require bot admin
        return BotConfiguration.AdminUsers.Contains(userId.ToString());
    }
    
    #endregion

}