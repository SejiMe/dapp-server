using System.ComponentModel.DataAnnotations;
using dengue.watch.api.common.exceptions;
using dengue.watch.api.common.interfaces;
using dengue.watch.api.common.services;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.auth;

/// <summary>
/// User registration endpoint
/// </summary>
public class RegisterEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", HandleAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .WithDescription("Creates a new user account with email and password")
            .WithTags("Authentication")
            .WithOpenApi()
            .Produces<RegisterResponse>(200)
            .Produces(400)
            .Produces(409);
        
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] RegisterRequest request,
        [FromServices] Supabase.Client supabaseClient,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ILogger<RegisterEndpoint> logger)
    {
        try
        {
            logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // Validate request
            if (!ModelState.IsValid(request, out var validationErrors))
            {
                throw new RegistrationFailedException($"Invalid registration data. {validationErrors}");
            }

            // Attempt registration with Supabase
            var response = await supabaseClient.Auth.SignUp(request.Email, request.Password);

            if (response?.User == null)
            {
                throw new RegistrationFailedException("Registration failed", "Supabase registration returned null user");
            }

            logger.LogInformation("User registered successfully: {UserId}", response.User?.Id);

            // Check if email confirmation is required
            if (response.User == null)
            {
                logger.LogInformation("Email confirmation required for user: {Email}", request.Email);
                return Results.Ok(new RegisterResponse
                {
                    Success = true,
                    Message = "Registration successful. Please check your email for confirmation.",
                    RequiresEmailConfirmation = true,
                    UserId = response.User?.Id ?? string.Empty
                });
            }

            // Generate our own JWT tokens
            var tokenPair = jwtTokenService.GenerateTokens(
                response.User?.Id ?? string.Empty, 
                response.User?.Email ?? request.Email,
                new Dictionary<string, string>
                {
                    ["email_confirmed"] = (response.User?.EmailConfirmedAt.HasValue ?? false).ToString().ToLowerInvariant()
                });

            logger.LogInformation("JWT tokens generated for new user: {UserId}", response.User?.Id);

            return Results.Ok(new RegisterResponse
            {
                Success = true,
                Message = "Registration successful",
                AccessToken = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                ExpiresIn = tokenPair.ExpiresIn,
                TokenType = tokenPair.TokenType,
                RequiresEmailConfirmation = false,
                UserId = response.User?.Id ?? string.Empty,
                User = new UserInfo
                {
                    Id = response.User?.Id ?? string.Empty,
                    Email = response.User?.Email ?? request.Email,
                    EmailConfirmed = response.User?.EmailConfirmedAt.HasValue ?? false,
                    CreatedAt = response.User?.CreatedAt ?? DateTimeOffset.UtcNow
                }
            });
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex) when (ex.Message.Contains("already registered"))
        {
            logger.LogWarning("Registration failed - user already exists: {Email}", request.Email);
            throw new UserAlreadyExistsException(request.Email);
        }
        catch (AuthException)
        {
            // Re-throw auth exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during registration for email: {Email}", request.Email);
            throw new RegistrationFailedException("An unexpected error occurred during registration", ex.Message, ex);
        }
    }
}

/// <summary>
/// Registration request model
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    [StringLength(254, ErrorMessage = "Email address is too long")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 128 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", 
    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

/// <summary>
/// Registration response model
/// </summary>
public record RegisterResponse
{
    /// <summary>
    /// Whether registration was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Access token (if registration is complete)
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh token (if registration is complete)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int? ExpiresIn { get; init; }

    /// <summary>
    /// Token type
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// Whether email confirmation is required
    /// </summary>
    public bool RequiresEmailConfirmation { get; init; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// User information (if registration is complete)
    /// </summary>
    public UserInfo? User { get; init; }
}


