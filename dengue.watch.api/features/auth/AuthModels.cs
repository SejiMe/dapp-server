using System.ComponentModel.DataAnnotations;

namespace dengue.watch.api.features.auth;

/// <summary>
/// User information model
/// </summary>
public record UserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// User email
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Whether email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// Account creation date
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Last sign-in date
    /// </summary>
    public DateTimeOffset? LastSignInAt { get; init; }
}

/// <summary>
/// Helper class for model validation
/// </summary>
public static class ModelState
{
    public static bool IsValid<T>(T model, out string validationErrors)
    {
        validationErrors = string.Empty;
        var context = new ValidationContext(model ?? throw new ArgumentNullException(nameof(model)));
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(model, context, results, true);
        
        if (!isValid)
        {
            validationErrors = string.Join("; ", results.Select(r => r.ErrorMessage));
        }
        
        return isValid;
    }
}
