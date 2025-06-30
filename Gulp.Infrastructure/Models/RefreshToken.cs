using System.ComponentModel.DataAnnotations;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Models;

public class RefreshToken : BaseEntity
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string JwtId { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
    
    [StringLength(200)]
    public string? RevokedReason { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation properties
    public virtual Data.ApplicationUser User { get; set; } = null!;
    
    // Helper properties
    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    public bool IsActive => !IsRevoked && !IsExpired && !IsDeleted;
}
