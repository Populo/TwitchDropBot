using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;
using TwitchDropBot.Bot.Helpers;
using TwitchDropBot.Data;

namespace TwitchDropBot.Bot.Commands;

public static class Commands
{
    #region Builders
    
    private static SlashCommandBuilder ignoreBuilder = new()
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
    
    private static SlashCommandBuilder ignoredGamesBuilder = new()
    {
        Name = "ignored-games",
        Description = "Show a list of all ignored games",
    };
    
    private static SlashCommandBuilder nonIgnoredGamesBuilder = new()
    {
        Name = "allowed-games",
        Description = "Show a list of all allowed games",
    };

    public static List<SlashCommandBuilder> CommandBuilders =
        [ignoreBuilder, ignoredGamesBuilder, nonIgnoredGamesBuilder];

    #endregion
    
    #region Command
    
    public static async Task IgnoreGame(SocketSlashCommand arg, DiscordSocketClient client)
    {
        await arg.DeferAsync();
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("You are not allowed to use this command", ephemeral: true);
            var channel = await client.GetChannelAsync(BotConfiguration.ErrorChannel) as ITextChannel
                ?? throw new Exception("Error channel not found");
            
            await channel.SendMessageAsync($"User {arg.User.Username} tried to use Ignore command");
            return;
        }
        
        var gameId = arg.Data.Options.First(o => o.Name == "game").Value.ToString();
        var ignore = (bool)arg.Data.Options.First(o => o.Name == "ignore").Value;
        
        await using var db = new DropContext();
        var game = db.Games.FirstOrDefault(g => g.Name == gameId);
        
        if (null == game) await arg.FollowupAsync("Game not found", ephemeral: true);
        
        game!.Ignored = ignore;
        await db.SaveChangesAsync();
        
        await arg.FollowupAsync($"Game {game.Name} {(ignore ? "ignored" : "allowed")}");
    }

    public static async Task ListGames(SocketSlashCommand arg, bool ignored)
    {
        await arg.DeferAsync();
        
        await using var db = new DropContext();
        var games = db.Games.Where(g => g.Ignored == ignored).ToList();

        var message = games.Any()
            ? string.Join("\n", games.Select(g => $"`{g.Name}`"))
            : "No games found.";
        
        await arg.FollowupAsync(message);
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