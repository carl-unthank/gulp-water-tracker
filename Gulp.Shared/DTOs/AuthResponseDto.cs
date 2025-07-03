namespace Gulp.Shared.DTOs;

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public List<string> Roles { get; set; } = [];
}
