using Gulp.Infrastructure.Data;

namespace Gulp.Infrastructure.Services;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry);

public interface IJwtService
{
    Task<TokenPair> GenerateTokenPairAsync(ApplicationUser user, int? customUserId = null);
    Task<bool> ValidateAccessTokenAsync(string token);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task<TokenPair?> RefreshTokenPairAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    string? GetUserIdFromToken(string token);
    Task<bool> IsTokenRevokedAsync(string jti);
}
