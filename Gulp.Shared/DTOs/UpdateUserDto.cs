using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.DTOs;

public class UpdateUserDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }
}
