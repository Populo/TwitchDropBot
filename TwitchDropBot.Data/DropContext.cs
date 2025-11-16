using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TwitchDropBot.Data;

public class DropContext : DbContext
{
    public DbSet<DropDto> Drops { get; set; }
    public DbSet<Game> Games { get; set; }
    
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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Always include Game whenever you query Drops
        modelBuilder.Entity<DropDto>()
            .Navigation(d => d.Game)
            .AutoInclude();
    }
}

public class DropDto
{
    public string Id { get; set; }
    public Game Game { get; set; }
    public string CampaignName { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

public class Game
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Ignored { get; set; }
}

