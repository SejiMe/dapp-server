using System.ComponentModel.DataAnnotations;

namespace dengue.watch.api.common.options;

/// <summary>
/// Configuration options for Supabase connection
/// </summary>
public class SupabaseConfiguration
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Supabase";

    /// <summary>
    /// Supabase project URL
    /// </summary>
    [Required(ErrorMessage = "Supabase URL is required")]
    [Url(ErrorMessage = "Supabase URL must be a valid URL")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Supabase anonymous key
    /// </summary>
    [Required(ErrorMessage = "Supabase anonymous key is required")]
    [MinLength(1, ErrorMessage = "Supabase anonymous key cannot be empty")]
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// Supabase JWT secret (optional, used for token validation)
    /// </summary>
    public string? JwtSecret { get; set; }

    /// <summary>
    /// Validate the configuration
    /// </summary>
    public void Validate()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(x => x.ErrorMessage));
            throw new InvalidOperationException($"Supabase configuration is invalid: {errors}");
        }

        // Additional custom validation
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new InvalidOperationException("Supabase URL must be a valid HTTP or HTTPS URL");
        }

        if (!Url.Contains("supabase.co") && !Url.Contains("localhost"))
        {
            throw new InvalidOperationException("Supabase URL should contain 'supabase.co' or be localhost for development");
        }
    }

    /// <summary>
    /// Get the authentication URL
    /// </summary>
    public string GetAuthUrl() => $"{Url.TrimEnd('/')}/auth/v1";

    /// <summary>
    /// Get the REST API URL
    /// </summary>
    public string GetRestUrl() => $"{Url.TrimEnd('/')}/rest/v1";

    /// <summary>
    /// Check if JWT secret is configured
    /// </summary>
    public bool HasJwtSecret => !string.IsNullOrWhiteSpace(JwtSecret);
}
