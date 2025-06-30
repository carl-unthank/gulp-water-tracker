using Microsoft.AspNetCore.Identity;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Data;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public string FullName => $"{FirstName} {LastName}";
}
