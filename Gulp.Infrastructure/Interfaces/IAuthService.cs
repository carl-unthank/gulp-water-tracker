using Gulp.Shared.DTOs;
using Gulp.Shared.Common;

namespace Gulp.Infrastructure.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<Result> LogoutAsync(int userId, string? refreshToken = null);
    Task<Result<UserDto>> GetCurrentUserAsync(int userId);
    Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<Result> ResetPasswordAsync(string email);
}
