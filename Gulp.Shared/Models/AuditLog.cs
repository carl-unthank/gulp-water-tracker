using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.Models;

public class AuditLog : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Details { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
