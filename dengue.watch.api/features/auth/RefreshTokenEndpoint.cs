using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using dengue.watch.api.common.exceptions;
using dengue.watch.api.common.interfaces;
using dengue.watch.api.common.services;
using Microsoft.AspNetCore.Mvc;

namespace dengue.watch.api.features.auth;

/// <summary>
/// Token refresh endpoint
/// </summary>
public class RefreshTokenEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", HandleAsync)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithDescription("Generates a new access token using a valid refresh token")
            .WithTags("Authentication")
            .WithOpenApi()
            .Produces<RefreshTokenResponse>(200)
            .Produces(400)
            .Produces(401);
        
        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromBody] RefreshTokenRequest request,
        [FromServices] Supabase.Client supabaseClient,
        [FromServices] IJwtTokenService jwtTokenService,
        [FromServices] ILogger<RefreshTokenEndpoint> logger)
    {
        try
        {
            logger.LogInformation("Token refresh attempt");

            // Validate request
            if (!ModelState.IsValid(request, out var validationErrors))
            {
                throw new TokenException("Invalid refresh token request", validationErrors);
            }

            // Get refresh token from HttpOnly cookie
            if (!httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) || 
                string.IsNullOrWhiteSpace(refreshToken))
            {
                logger.LogWarning("Token refresh failed - No refresh token cookie");
                throw new InvalidTokenException("No refresh token provided");
            }

            // Validate refresh token
            var refreshTokenClaims = jwtTokenService.ValidateRefreshToken(refreshToken);
            if (refreshTokenClaims == null)
            {
                logger.LogWarning("Token refresh failed - Invalid refresh token");
                throw new InvalidTokenException("Invalid or expired refresh token");
            }

            // Check if refresh token is revoked
            if (jwtTokenService.IsRefreshTokenRevoked(request.RefreshToken))
            {
                logger.LogWarning("Token refresh failed - Refresh token is revoked");
                throw new InvalidTokenException("Refresh token has been revoked");
            }

            // Extract user information from refresh token
            var userId = refreshTokenClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = refreshTokenClaims.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("Token refresh failed - Missing user information in refresh token");
                throw new InvalidTokenException("Invalid refresh token - missing user information");
            }

            logger.LogInformation("Refreshing token for user: {UserId}", userId);

            // Optionally, verify user still exists and is active in Supabase
            // This is optional but adds extra security
            try
            {
                var supabaseUser = await GetUserFromSupabase(supabaseClient, userId);
                if (supabaseUser == null)
                {
                    logger.LogWarning("Token refresh failed - User not found in Supabase: {UserId}", userId);
                    throw new UserNotFoundException(userId);
                }

                // Update email and other claims if they've changed
                userEmail = supabaseUser.Email ?? userEmail;
                
                var additionalClaims = new Dictionary<string, string>
                {
                    ["email_confirmed"] = supabaseUser.EmailConfirmedAt.HasValue.ToString().ToLowerInvariant(),
                    ["token_refreshed_at"] = DateTimeOffset.UtcNow.ToString("O")
                };

                // Generate new token pair
                var newTokenPair = jwtTokenService.GenerateTokens(userId, userEmail, additionalClaims);

                // Revoke the old refresh token
                jwtTokenService.RevokeRefreshToken(refreshToken);

                logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

                jwtTokenService.SetRefreshTokenToCookie(newTokenPair.RefreshToken, false);

                return Results.Ok(new RefreshTokenResponse
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newTokenPair.AccessToken,
                    // RefreshToken = newTokenPair.RefreshToken,
                    ExpiresIn = newTokenPair.ExpiresIn,
                    TokenType = newTokenPair.TokenType,
                    RefreshedAt = DateTimeOffset.UtcNow,
                    User = new UserInfo
                    {
                        Id = userId,
                        Email = userEmail,
                        EmailConfirmed = supabaseUser.EmailConfirmedAt.HasValue,
                        CreatedAt = supabaseUser.CreatedAt,
                        LastSignInAt = supabaseUser.LastSignInAt
                    }
                });
            }
            catch (AuthException)
            {
                // Re-throw auth exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to verify user in Supabase during token refresh for user: {UserId}", userId);
                
                // If Supabase verification fails, we can still refresh the token
                // using the information from the refresh token
                var basicClaims = new Dictionary<string, string>
                {
                    ["token_refreshed_at"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["supabase_verification"] = "failed"
                };

                var fallbackTokenPair = jwtTokenService.GenerateTokens(userId, userEmail, basicClaims);
                jwtTokenService.RevokeRefreshToken(refreshToken);

                logger.LogInformation("Token refreshed with fallback method for user: {UserId}", userId);

                return Results.Ok(new RefreshTokenResponse
                {
                    Success = true,
                    Message = "Token refreshed successfully (with limited verification)",
                    AccessToken = fallbackTokenPair.AccessToken,
                    RefreshToken = fallbackTokenPair.RefreshToken,
                    ExpiresIn = fallbackTokenPair.ExpiresIn,
                    TokenType = fallbackTokenPair.TokenType,
                    RefreshedAt = DateTimeOffset.UtcNow,
                    User = new UserInfo
                    {
                        Id = userId,
                        Email = userEmail,
                        EmailConfirmed = false, // We can't verify this without Supabase
                        CreatedAt = DateTimeOffset.MinValue,
                        LastSignInAt = null
                    }
                });
            }
        }
        catch (AuthException)
        {
            // Re-throw auth exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during token refresh");
            throw new TokenException("An unexpected error occurred during token refresh", ex.Message, ex);
        }
    }

    private static async Task<Supabase.Gotrue.User?> GetUserFromSupabase(Supabase.Client supabaseClient, string userId)
    {
        try
        {
            // Get current user from Supabase
            var currentUser = supabaseClient.Auth.CurrentUser;
            if (currentUser?.Id == userId)
            {
                return currentUser;
            }

            // If no current user or different user, we can't easily verify
            // without more complex Supabase admin operations
            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Refresh token request model
/// </summary>
public record RefreshTokenRequest
{
    /// <summary>
    /// Refresh token to use for generating new access token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Refresh token response model
/// </summary>
public record RefreshTokenResponse
{
    /// <summary>
    /// Whether token refresh was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// New access token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// New refresh token
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
    /// Timestamp when token was refreshed
    /// </summary>
    public DateTimeOffset RefreshedAt { get; init; }

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; init; } = new();
}


