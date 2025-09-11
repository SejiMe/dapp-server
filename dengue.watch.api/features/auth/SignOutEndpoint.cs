using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using dengue.watch.api.common.exceptions;
using dengue.watch.api.common.interfaces;
using dengue.watch.api.common.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.auth;

/// <summary>
/// User sign-out endpoint
/// </summary>
public class SignOutEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/signout", HandleAsync)
            .WithName("SignOut")
            .WithSummary("Sign out and invalidate tokens")
            .WithDescription("Signs out the user and invalidates their refresh token")
            .WithTags("Authentication")
            .WithOpenApi()
            .RequireAuthorization() // Requires valid JWT token
            .Produces<SignOutResponse>(200)
            .Produces(400)
            .Produces(401);
        
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] SignOutRequest request,
        [FromServices] Supabase.Client supabaseClient,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ILogger<SignOutEndpoint> logger,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;

            logger.LogInformation("Sign-out attempt for user: {UserId} ({Email})", userId, userEmail);

            // Validate request
            if (!ModelState.IsValid(request, out var validationErrors))
            {
                throw new AuthenticationFailedException("Invalid sign-out data", validationErrors);
            }

            // Validate refresh token before revoking
            var refreshTokenClaims = jwtTokenService.ValidateRefreshToken(request.RefreshToken);
            if (refreshTokenClaims == null)
            {
                logger.LogWarning("Sign-out failed for user: {UserId} - Invalid refresh token", userId);
                throw new InvalidTokenException("Invalid refresh token");
            }

            // Ensure the refresh token belongs to the authenticated user
            var refreshTokenUserId = refreshTokenClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (refreshTokenUserId != userId)
            {
                logger.LogWarning("Sign-out failed for user: {UserId} - Refresh token belongs to different user: {RefreshTokenUserId}", 
                    userId, refreshTokenUserId);
                throw new InvalidTokenException("Refresh token does not belong to authenticated user");
            }

            // Revoke the refresh token
            jwtTokenService.RevokeRefreshToken(request.RefreshToken);

            // Sign out from Supabase (optional, depending on your needs)
            try
            {
                await supabaseClient.Auth.SignOut();
                logger.LogDebug("Successfully signed out from Supabase for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                // Log but don't fail the entire operation if Supabase sign-out fails
                logger.LogWarning(ex, "Failed to sign out from Supabase for user: {UserId}, but continuing with local sign-out", userId);
            }

            logger.LogInformation("User signed out successfully: {UserId}", userId);

            return Results.Ok(new SignOutResponse
            {
                Success = true,
                Message = "Sign-out successful",
                SignedOutAt = DateTimeOffset.UtcNow
            });
        }
        catch (AuthException)
        {
            // Re-throw auth exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogError(ex, "Unexpected error during sign-out for user: {UserId}", userId);
            throw new AuthenticationFailedException("An unexpected error occurred during sign-out", ex.Message, ex);
        }
    }
}

/// <summary>
/// Sign-out request model
/// </summary>
public record SignOutRequest
{
    /// <summary>
    /// Refresh token to invalidate
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Whether to sign out from all devices (future feature)
    /// </summary>
    public bool SignOutFromAllDevices { get; init; } = false;
}

/// <summary>
/// Sign-out response model
/// </summary>
public record SignOutResponse
{
    /// <summary>
    /// Whether sign-out was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when user was signed out
    /// </summary>
    public DateTimeOffset SignedOutAt { get; init; }
}
