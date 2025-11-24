namespace TwitchDropBot.Bot.Helpers;


public static class BotConfiguration
{
    private static List<string> GetConfiguredList(string envVarName, string errorMessage)
    {
        var value = Environment.GetEnvironmentVariable(envVarName)
                    ?? throw new ArgumentNullException(envVarName, errorMessage);
        return value.Split(',').ToList();
    }
    
    public static List<string> PostChannels => GetConfiguredList("PostChannelId", "Invalid post channel id. set value in compose file");
    public static List<string> ErrorChannels => GetConfiguredList("ErrorChannelId", "Invalid error channel id. set value in compose file");
    public static List<string> AdminUsers => GetConfiguredList("BotAdminUsers", "Invalid admin users. set value in compose file");
}