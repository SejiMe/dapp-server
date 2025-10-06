
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

    public DbSet<WeatherCode> WeatherCodes => Set<WeatherCode>();
    public DbSet<DailyWeather> DailyWeather => Set<DailyWeather>();
    public DbSet<AdministrativeArea> AdministrativeAreas { get; set; } = null!;
    public DbSet<WeeklyDengueCase> WeeklyDengueCases => Set<WeeklyDengueCase>();
    public DbSet<MonthlyDengueCase> MonthlyDengueCases => Set<MonthlyDengueCase>();

    public DbSet<PredictedWeeklyDengueCase> PredictedWeeklyDengues => Set<PredictedWeeklyDengueCase>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure new entities
        modelBuilder.ConfigureEntities();

        // Apply configurations from all assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
