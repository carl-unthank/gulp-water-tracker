using Gulp.Shared.DTOs;

namespace Gulp.Client.Services;

public interface IAuthStateService
{
    event Action<bool>? AuthStateChanged;

    bool IsAuthenticated { get; }
    UserDto? CurrentUser { get; }
    string? LastErrorMessage { get; }

    Task<bool> LoginAsync(LoginDto loginDto);
    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    Task InitializeAsync();
}
