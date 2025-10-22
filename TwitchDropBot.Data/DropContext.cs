using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TwitchDropBot.Data;

public class DropContext : DbContext
{
    public DbSet<DropDto> Drops { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
    {
        if (optionsBuilder.IsConfigured) return;
        
        var connectionString = new SqlConnectionStringBuilder()
        {
            DataSource = "dale-server",
            InitialCatalog = "db_twitchbot_prod",
            UserID = "user_twitchbot_prod",
            TrustServerCertificate = true,  
            Encrypt = true,  
            Password = File.ReadAllText("/run/secrets/dbPass")
        }.ConnectionString;

        optionsBuilder.UseSqlServer(connectionString,
            options => { 
                options.EnableRetryOnFailure(
                    maxRetryCount: 20, 
                    maxRetryDelay: TimeSpan.FromSeconds(10), 
                    errorNumbersToAdd: null
                ); 
            });
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

