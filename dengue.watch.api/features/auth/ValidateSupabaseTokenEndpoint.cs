using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace dengue.watch.api.features.auth;

/// <summary>
/// Endpoint to validate Supabase JWT token and generate backend-specific token
/// </summary>
public class ValidateSupabaseTokenEndpoint : IEndpoint
{
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/supabase-validate", HandleAsync)
            .WithName("ValidateSupabaseToken")
            .WithSummary("Validate Supabase JWT and generate backend token")
            .WithDescription("Validates a Supabase JWT token and returns a backend-specific token")
            .WithTags("Authentication")
            .WithOpenApi()
            .Produces<SupabaseValidationResponse>(200)
            .Produces(400)
            .Produces(401);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] SupabaseValidationRequest request,
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<ValidateSupabaseTokenEndpoint> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Validating Supabase token for email: {Email}", request.Email);

            if (string.IsNullOrWhiteSpace(request.SupabaseToken))
            {
                return Results.BadRequest(new SupabaseValidationResponse
                {
                    Success = false,
                    Message = "Supabase token is required"
                });
            }

            // Get JWT secret from configuration
            var jwtSecret = configuration["Supabase:JwtSecret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                logger.LogWarning("JWT secret not configured - using Supabase token directly");
                
                // Parse Supabase token to get user info
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(request.SupabaseToken);
                    
                    var userId = jsonToken.Subject;
                    var emailClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
                        ?? jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                        ?? request.Email;
                    var role = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value 
                        ?? jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                        ?? "user";

                    return Results.Ok(new SupabaseValidationResponse
                    {
                        Success = true,
                        Message = "Token validated (no backend token generated)",
                        AccessToken = request.SupabaseToken,
                        ExpiresIn = jsonToken.Payload.Exp.HasValue 
                            ? jsonToken.Payload.Exp.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds() 
                            : 3600,
                        TokenType = "Bearer",
                        User = new AuthUserInfo
                        {
                            Id = userId ?? string.Empty,
                            Email = emailClaim ?? string.Empty,
                            Role = role
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to parse Supabase token");
                    return Results.BadRequest(new SupabaseValidationResponse
                    {
                        Success = false,
                        Message = "Invalid Supabase token format"
                    });
                }
            }

            // Validate token with JWT secret
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            try
            {
                tokenHandler.ValidateToken(request.SupabaseToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Subject;
                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value 
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                    ?? request.Email;
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value 
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                    ?? "user";

                logger.LogInformation("Supabase token validated successfully for user: {UserId}", userId);

                // Return success with the validated token info
                return Results.Ok(new SupabaseValidationResponse
                {
                    Success = true,
                    Message = "Token validated successfully",
                    AccessToken = request.SupabaseToken,
                    ExpiresIn = jwtToken.Payload.Exp.HasValue
                        ? jwtToken.Payload.Exp.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        : 3600,
                    TokenType = "Bearer",
                    User = new AuthUserInfo
                    {
                        Id = userId ?? string.Empty,
                        Email = emailClaim ?? string.Empty,
                        Role = role
                    }
                });
            }
            catch (SecurityTokenExpiredException)
            {
                logger.LogWarning("Supabase token has expired");
                return Results.Json(new SupabaseValidationResponse
                {
                    Success = false,
                    Message = "Token has expired"
                }, statusCode: 401);
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                logger.LogWarning("Invalid Supabase token signature");
                return Results.Json(new SupabaseValidationResponse
                {
                    Success = false,
                    Message = "Invalid token signature"
                }, statusCode: 401);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token validation failed");
                return Results.Json(new SupabaseValidationResponse
                {
                    Success = false,
                    Message = "Token validation failed"
                }, statusCode: 401);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during Supabase token validation");
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

/// <summary>
/// Request model for Supabase token validation
/// </summary>
public record SupabaseValidationRequest
{
    /// <summary>
    /// Supabase JWT token
    /// </summary>
    public required string SupabaseToken { get; init; }
    
    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
/// Response model for Supabase token validation
/// </summary>
public record SupabaseValidationResponse
{
    /// <summary>
    /// Whether validation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Access token (either backend-generated or Supabase token)
    /// </summary>
    public string? AccessToken { get; init; }
    
    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public long ExpiresIn { get; init; }
    
    /// <summary>
    /// Token type
    /// </summary>
    public string TokenType { get; init; } = "Bearer";
    
    /// <summary>
    /// User information
    /// </summary>
    public AuthUserInfo? User { get; init; }
}

/// <summary>
/// User information model for auth responses
/// </summary>
public record AuthUserInfo
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
    /// User role
    /// </summary>
    public string Role { get; init; } = "user";
}
