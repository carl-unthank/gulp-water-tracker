using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.DTOs;

public class AdminUpdateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public bool EmailConfirmed { get; set; }
    
    public List<string>? Roles { get; set; }
}
