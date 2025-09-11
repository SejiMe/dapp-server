using System.ComponentModel.DataAnnotations;
using dengue.watch.api.common.exceptions;
using dengue.watch.api.common.interfaces;
using dengue.watch.api.common.services;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.auth;

/// <summary>
/// User sign-in endpoint
/// </summary>
public class SignInEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signin", HandleAsync)
            .WithName("SignIn")
            .WithSummary("Sign in with email and password")
            .WithDescription("Authenticates a user and returns JWT tokens")
            .WithTags("Authentication")
            .WithOpenApi()
            .Produces<SignInResponse>(200)
            .Produces(400)
            .Produces(401);
        
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] SignInRequest request,
        [FromServices] Supabase.Client supabaseClient,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ILogger<SignInEndpoint> logger)
    {
        try
        {
            logger.LogInformation("Sign-in attempt for email: {Email}", request.Email);

            // Validate request
            if (!ModelState.IsValid(request, out var validationErrors))
            {
                throw new AuthenticationFailedException("Invalid sign-in data", validationErrors);
            }

            // Attempt sign-in with Supabase
            var response = await supabaseClient.Auth.SignIn(request.Email, request.Password);

            if (response?.User == null)
            {
                logger.LogWarning("Sign-in failed for email: {Email} - Invalid credentials", request.Email);
                throw new AuthenticationFailedException("Invalid email or password");
            }

            // Check if email is confirmed (if required)
            if (!(response.User?.EmailConfirmedAt.HasValue ?? false) && IsEmailConfirmationRequired())
            {
                logger.LogWarning("Sign-in failed for email: {Email} - Email not confirmed", request.Email);
                throw new EmailConfirmationRequiredException();
            }

            logger.LogInformation("User signed in successfully: {UserId}", response.User?.Id);

            // Generate our own JWT tokens
            var tokenPair = jwtTokenService.GenerateTokens(
                response.User?.Id ?? string.Empty,
                response.User?.Email ?? request.Email,
                new Dictionary<string, string>
                {
                    ["email_confirmed"] = (response.User?.EmailConfirmedAt.HasValue ?? false).ToString().ToLowerInvariant(),
                    ["last_sign_in"] = response.User?.LastSignInAt?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O")
                });

            logger.LogInformation("JWT tokens generated for user: {UserId}", response.User?.Id);

            jwtTokenService.SetRefreshTokenToCookie(tokenPair.RefreshToken, request.RememberMe);

            return Results.Ok(new SignInResponse
            {
                Success = true,
                Message = "Sign-in successful",
                AccessToken = tokenPair.AccessToken,
                //RefreshToken = tokenPair.RefreshToken,
                ExpiresIn = tokenPair.ExpiresIn,
                TokenType = tokenPair.TokenType,
                User = new UserInfo
                {
                    Id = response.User?.Id ?? string.Empty,
                    Email = response.User?.Email ?? request.Email,
                    EmailConfirmed = response.User?.EmailConfirmedAt.HasValue ?? false,
                    CreatedAt = response.User?.CreatedAt ?? DateTimeOffset.UtcNow,
                    LastSignInAt = response.User?.LastSignInAt
                }
            });
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex) when (ex.Message.Contains("Invalid login credentials"))
        {
            logger.LogWarning("Sign-in failed for email: {Email} - Invalid credentials", request.Email);
            throw new AuthenticationFailedException("Invalid email or password");
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex) when (ex.Message.Contains("Email not confirmed"))
        {
            logger.LogWarning("Sign-in failed for email: {Email} - Email not confirmed", request.Email);
            throw new EmailConfirmationRequiredException();
        }
        catch (AuthException)
        {
            // Re-throw auth exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during sign-in for email: {Email}", request.Email);
            throw new AuthenticationFailedException("An unexpected error occurred during sign-in", ex.Message, ex);
        }
    }

    private static bool IsEmailConfirmationRequired()
    {
        // This could be configurable based on your requirements
        // For now, let's assume email confirmation is not strictly required
        return false;
    }
}

/// <summary>
/// Sign-in request model
/// </summary>
public record SignInRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Whether to remember the user (extends token lifetime)
    /// </summary>
    public bool RememberMe { get; init; } = false;
}

/// <summary>
/// Sign-in response model
/// </summary>
public record SignInResponse
{
    /// <summary>
    /// Whether sign-in was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Token type
    /// </summary>
    public string TokenType { get; init; } = string.Empty;

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; init; } = new();
}


