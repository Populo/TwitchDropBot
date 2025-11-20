using Microsoft.Extensions.Configuration;

namespace TwitchDropBot.Bot.Helpers;

public static class BotConfiguration
{
    private static readonly IConfiguration _configuration;

    static BotConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    public static List<string> PostChannels => _configuration["postChannel"]!.Split(',').ToList();
    public static List<string> ErrorChannels => _configuration["errorChannel"]!.Split(',').ToList();
    public static List<string> AdminUsers => _configuration["adminUsers"]!.Split(',').ToList();
}