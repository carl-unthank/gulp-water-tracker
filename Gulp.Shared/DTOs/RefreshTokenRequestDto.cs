using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.DTOs;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
