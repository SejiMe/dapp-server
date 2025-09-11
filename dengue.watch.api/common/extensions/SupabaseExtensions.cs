using dengue.watch.api.common.options;
using Microsoft.Extensions.Options;

namespace dengue.watch.api.common.extensions;

/// <summary>
/// Extension methods for configuring Supabase
/// </summary>
public static class SupabaseExtensions
{
    /// <summary>
    /// Add Supabase configuration with validation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSupabaseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options with validation
        services.Configure<SupabaseConfiguration>(configuration.GetSection(SupabaseConfiguration.SectionName));
        
        // Add options validation
        services.AddSingleton<IValidateOptions<SupabaseConfiguration>, SupabaseOptionsValidator>();
        
        // Validate on startup
        services.AddSingleton<IHostedService, SupabaseOptionsValidationService>();

        return services;
    }

    /// <summary>
    /// Add Supabase client for authentication
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSupabaseClient(this IServiceCollection services)
    {
        // Register Supabase client as singleton
        services.AddSingleton<Supabase.Client>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SupabaseConfiguration>>().Value;
            var logger = provider.GetRequiredService<ILogger<Supabase.Client>>();
            
            var supabaseOptions = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false, // We'll handle tokens manually
                AutoConnectRealtime = false // For API use, we don't need realtime
            };
            
            logger.LogInformation("Initializing Supabase client for URL: {Url}", options.Url);
            
            return new Supabase.Client(options.Url, options.AnonKey, supabaseOptions);
        });

        return services;
    }
}

/// <summary>
/// Validator for SupabaseConfiguration
/// </summary>
public class SupabaseOptionsValidator : IValidateOptions<SupabaseConfiguration>
{
    public ValidateOptionsResult Validate(string? name, SupabaseConfiguration options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Background service to validate Supabase options on startup
/// </summary>
public class SupabaseOptionsValidationService : BackgroundService
{
    private readonly IOptionsMonitor<SupabaseConfiguration> _options;
    private readonly ILogger<SupabaseOptionsValidationService> _logger;

    public SupabaseOptionsValidationService(
        IOptionsMonitor<SupabaseConfiguration> options,
        ILogger<SupabaseOptionsValidationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Validate options on startup
        try
        {
            var options = _options.CurrentValue;
            options.Validate();
            _logger.LogInformation("Supabase configuration validated successfully");
            _logger.LogInformation("Supabase URL: {Url}", options.Url);
            _logger.LogInformation("Has JWT Secret: {HasJwtSecret}", options.HasJwtSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase configuration validation failed");
            throw;
        }

        // This service completes immediately after validation
        await Task.CompletedTask;
    }
}


