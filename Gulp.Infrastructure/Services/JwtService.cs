using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Models;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GulpDbContext _context;
    private readonly ILogger<JwtService> _logger;
    private readonly RSA _rsa;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public JwtService(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        GulpDbContext context,
        ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
        _logger = logger;

        // Initialize RSA for RS256 signing
        _rsa = RSA.Create(2048);
        LoadOrGenerateRSAKey();

        _issuer = _configuration["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _audience = _configuration["JwtSettings:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        _accessTokenExpiryMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "15"); // Short-lived: 15 minutes
        _refreshTokenExpiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7"); // 7 days
    }

    public async Task<TokenPair> GenerateTokenPairAsync(ApplicationUser user, int? customUserId = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jti = Guid.NewGuid().ToString();
        var userIdForClaims = customUserId ?? user.Id; // Use custom user ID if provided
        var ppid = GeneratePPID(user.Id); // Still use ASP.NET Identity ID for PPID

        // 3.3 & 3.12: Minimal claims, no sensitive data
        var accessTokenClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdForClaims.ToString()), // Use custom user ID for NameIdentifier
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, ppid), // Keep PPID for external identification
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("token_use", "access"), // 3.3: Specify token type
            new("user_id", userIdForClaims.ToString()) // Internal use - custom user ID
        };

        // Add role claims
        foreach (var role in roles)
        {
            accessTokenClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        // 3.1: Use RS256 instead of HS256
        var rsaKey = new RsaSecurityKey(_rsa);
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var accessToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: accessTokenClaims,
            notBefore: DateTime.UtcNow,
            expires: accessTokenExpiry,
            signingCredentials: credentials
        );

        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

        // Generate secure refresh token
        var refreshToken = GenerateSecureRefreshToken();

        // Store refresh token in database for revocation capability
        // Note: RefreshToken.UserId should reference ASP.NET Identity user ID for token management
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id, // ASP.NET Identity user ID for refresh token tracking
            Token = refreshToken,
            JwtId = jti,
            ExpiryDate = refreshTokenExpiry,
            IpAddress = null, // Will be set by controller
            UserAgent = null  // Will be set by controller
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated token pair for user {UserId} with JTI {Jti}", user.Id, jti);

        return new TokenPair(accessTokenString, refreshToken, accessTokenExpiry, refreshTokenExpiry);
    }

    public async Task<bool> ValidateAccessTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var rsaKey = new RsaSecurityKey(_rsa);

            // 3.4: Validate token integrity and claims on every request
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaKey,
                ValidateIssuer = true, // 3.6: Always check issuer
                ValidIssuer = _issuer,
                ValidateAudience = true, // 3.6: Always check audience
                ValidAudience = _audience,
                ValidateLifetime = true, // 3.7: Handle time-based claims carefully
                ClockSkew = TimeSpan.Zero, // No clock skew tolerance
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // 3.3: Verify token type
            var tokenUse = principal.FindFirst("token_use")?.Value;
            if (tokenUse != "access")
            {
                _logger.LogWarning("Invalid token type: {TokenUse}", tokenUse);
                return false;
            }

            // 3.13: Check if token is revoked
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && await IsTokenRevokedAsync(jti))
            {
                _logger.LogWarning("Token with JTI {Jti} is revoked", jti);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            return tokenEntity != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed");
            return false;
        }
    }

    public async Task<TokenPair?> RefreshTokenPairAsync(string refreshToken)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            if (tokenEntity == null)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                return null;
            }

            // Revoke the old refresh token
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedReason = "Used for refresh";

            // Find the custom user record to get the custom user ID
            var customUser = await _context.Users.FirstOrDefaultAsync(u => u.AspNetUserId == tokenEntity.User.Id);
            var customUserId = customUser?.Id;

            // Generate new token pair with custom user ID
            var newTokenPair = await GenerateTokenPairAsync(tokenEntity.User, customUserId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refreshed token pair for user {UserId}", tokenEntity.UserId);

            return newTokenPair;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token pair");
            return null;
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenEntity = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity != null)
            {
                tokenEntity.IsRevoked = true;
                tokenEntity.RevokedAt = DateTime.UtcNow;
                tokenEntity.RevokedReason = "Manual revocation";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked refresh token for user {UserId}", tokenEntity.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            // Return the actual user_id, not the PPID
            return jsonToken.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting user ID from token");
            return null;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string jti)
    {
        try
        {
            return await _context.RefreshTokens
                .AnyAsync(rt => rt.JwtId == jti && rt.IsRevoked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation status");
            return true; // Fail secure
        }
    }

    // Helper methods
    private void LoadOrGenerateRSAKey()
    {
        try
        {
            // In production, load from secure key storage (Azure Key Vault, etc.)
            var keyPath = _configuration["JwtSettings:RSAKeyPath"];
            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                var keyData = File.ReadAllText(keyPath);
                _rsa.ImportRSAPrivateKey(Convert.FromBase64String(keyData), out _);
                _logger.LogInformation("Loaded RSA key from file");
            }
            else
            {
                // Generate new key for development
                _logger.LogWarning("Generating new RSA key - this should not happen in production");
                // Key is already generated in RSA.Create(2048)
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading RSA key, using generated key");
        }
    }

    private string GeneratePPID(int userId)
    {
        // 3.10: Generate Pairwise Pseudonymous Identifier
        // This creates a unique identifier per user per application
        var input = $"{userId}:{_audience}:{_issuer}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash)[..16]; // Take first 16 characters
    }

    private static string GenerateSecureRefreshToken()
    {
        // Generate cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
}
