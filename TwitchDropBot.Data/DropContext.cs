using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TwitchDropBot.Data;

public class DropContext : DbContext
{
    public DbSet<DropDto> Drops { get; set; }
    public DbSet<Game> Games { get; set; }
    
    public static string GetMasterConnectionString()
    {
        return new SqlConnectionStringBuilder()
        {
            DataSource = Environment.GetEnvironmentVariable("DbHost"),
            InitialCatalog = "master",
            UserID = "sa",
            TrustServerCertificate = true,
            Encrypt = true,
            Password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD")
        }.ConnectionString;
    }

    private static string GetAppConnectionString()
    {
        return new SqlConnectionStringBuilder()
        {
            DataSource = Environment.GetEnvironmentVariable("DbHost"),
            InitialCatalog = Environment.GetEnvironmentVariable("DbName"),
            UserID = Environment.GetEnvironmentVariable("DbUser"),
            TrustServerCertificate = true,
            Encrypt = true,
            Password = Environment.GetEnvironmentVariable("DbPassword")
        }.ConnectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder = null)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.UseSqlServer(GetAppConnectionString(),
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

