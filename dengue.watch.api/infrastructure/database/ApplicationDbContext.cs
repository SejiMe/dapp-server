using Microsoft.EntityFrameworkCore;
using dengue.watch.api.features.denguealerts;

namespace dengue.watch.api.infrastructure.database;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets for entities
    public DbSet<DengueAlert> DengueAlerts => Set<DengueAlert>();
    public DbSet<WeatherCode> WeatherCodes => Set<WeatherCode>();
    public DbSet<DailyWeather> DailyWeather => Set<DailyWeather>();
    public DbSet<AdministrativeArea> AdministrativeAreas { get; set; } = null!;
    public DbSet<WeeklyDengueCase> WeeklyDengueCases => Set<WeeklyDengueCase>();
    public DbSet<MonthlyDengueCase> MonthlyDengueCases => Set<MonthlyDengueCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure DengueAlert entity
        modelBuilder.Entity<DengueAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Level).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.IsActive);
        });
        
        // Configure new entities
        modelBuilder.ConfigureEntities();

        // Apply configurations from all assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
