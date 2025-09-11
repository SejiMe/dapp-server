using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using dengue.watch.api.common.options;

namespace dengue.watch.api.common.services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access and refresh tokens for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="email">User email</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>Token pair with access and refresh tokens</returns>
    TokenPair GenerateTokens(string userId, string email, Dictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Validate and get claims from access token
    /// </summary>
    /// <param name="token">Access token to validate</param>
    /// <returns>Claims principal if valid, null if invalid</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Validate refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>Claims principal if valid, null if invalid</returns>
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="refreshToken">Token to revoke</param>
    void RevokeRefreshToken(string refreshToken);

    /// <summary>
    /// Check if refresh token is revoked
    /// </summary>
    /// <param name="refreshToken">Token to check</param>
    /// <returns>True if revoked, false otherwise</returns>
    bool IsRefreshTokenRevoked(string refreshToken);

    /// <summary>
    /// Set refresh token to cookie
    /// </summary>
    /// <param name="refreshToken">Refresh token to set</param>
    /// <param name="rememberMe">Whether to remember the user</param>
    void SetRefreshTokenToCookie(string refreshToken, bool rememberMe);

    void ClearRefreshTokenFromCookie();
}

/// <summary>
/// JWT token service implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly SupabaseConfiguration _supabaseConfig;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly HashSet<string> _revokedTokens = new();
    private readonly object _lockObject = new();

    private readonly IHttpContextAccessor _httpContextAccessor;

    // Token lifetimes
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(5);

    public JwtTokenService(IOptions<SupabaseConfiguration> supabaseConfig, ILogger<JwtTokenService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _supabaseConfig = supabaseConfig.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        if (string.IsNullOrEmpty(_supabaseConfig.JwtSecret))
        {
            throw new InvalidOperationException("JWT Secret is required for token generation");
        }
    }

    public void SetRefreshTokenToCookie(string refreshToken, bool rememberMe)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _httpContextAccessor!.HttpContext?.Request.IsHttps ?? false,
            SameSite = SameSiteMode.Strict,
            Expires = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddMinutes(5)
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);    
    }

    public void ClearRefreshTokenFromCookie()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("refresh_token");
    }

    public TokenPair GenerateTokens(string userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        var jti = Guid.NewGuid().ToString(); // Unique token identifier
        var refreshTokenId = Guid.NewGuid().ToString();

        var accessToken = GenerateAccessToken(userId, email, jti, additionalClaims);
        var refreshToken = GenerateRefreshToken(userId, email, refreshTokenId, jti);

        _logger.LogInformation("Generated tokens for user {UserId} with JTI {Jti}", userId, jti);

        return new TokenPair
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)AccessTokenLifetime.TotalSeconds,
            TokenType = "Bearer",
            AccessTokenExpiry = DateTime.UtcNow.Add(AccessTokenLifetime),
            RefreshTokenExpiry = DateTime.UtcNow.Add(RefreshTokenLifetime)
        };
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_supabaseConfig.JwtSecret!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _supabaseConfig.GetAuthUrl(),
                ValidateAudience = true,
                ValidAudience = "dengue-watch-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Access token validation failed");
            return null;
        }
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        try
        {
            lock (_lockObject)
            {
                if (_revokedTokens.Contains(refreshToken))
                {
                    _logger.LogWarning("Attempted to use revoked refresh token");
                    return null;
                }
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_supabaseConfig.JwtSecret!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _supabaseConfig.GetAuthUrl(),
                ValidateAudience = true,
                ValidAudience = "dengue-watch-api-refresh",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed");
            return null;
        }
    }

    public void RevokeRefreshToken(string refreshToken)
    {
        lock (_lockObject)
        {
            _revokedTokens.Add(refreshToken);
            _logger.LogInformation("Refresh token revoked");
        }
    }

    public bool IsRefreshTokenRevoked(string refreshToken)
    {
        lock (_lockObject)
        {
            return _revokedTokens.Contains(refreshToken);
        }
    }

    private string GenerateAccessToken(string userId, string email, string jti, Dictionary<string, string>? additionalClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("token_type", "access")
        };

        // Add additional claims if provided
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        return GenerateToken(claims, AccessTokenLifetime, "dengue-watch-api");
    }

    private string GenerateRefreshToken(string userId, string email, string refreshTokenId, string accessTokenJti)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, refreshTokenId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("token_type", "refresh"),
            new("access_token_jti", accessTokenJti) // Link to access token
        };

        return GenerateToken(claims, RefreshTokenLifetime, "dengue-watch-api-refresh");
    }

    private string GenerateToken(List<Claim> claims, TimeSpan lifetime, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_supabaseConfig.JwtSecret!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _supabaseConfig.GetAuthUrl(),
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Token pair containing access and refresh tokens
/// </summary>
public record TokenPair
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public DateTime RefreshTokenExpiry { get; init; }
}
