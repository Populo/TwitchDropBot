using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace TwitchDropBot.Data;

public class DropContext : DbContext
{
    public DbSet<DropDto> Drops { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
    {
        if (optionsBuilder.IsConfigured) return;
        
        var connection = new MySqlConnectionStringBuilder
        {
            Server = "dale-server",
            Database = "TwitchDropBot",
            UserID = "TwitchDropBot",
            Password = File.ReadAllText("/run/secrets/dbPass")
        };

        optionsBuilder.UseMySql(connection.ConnectionString,
            ServerVersion.AutoDetect(connection.ConnectionString),
            options => { options.EnableRetryOnFailure(20, TimeSpan.FromSeconds(10), new List<int>()); });
    }
}

public class DropDto
{
    public string Id { get; set; }
    public string GameId { get; set; }
    public string CampaignName { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

