using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.Models;

public class User : BaseEntity
{
    [Required]
    public int AspNetUserId { get; set; }

    // Navigation properties - ApplicationUser will be configured in DbContext
    public virtual ICollection<WaterIntake> WaterIntakes { get; set; } = new List<WaterIntake>();
    public virtual ICollection<DailyGoal> DailyGoals { get; set; } = new List<DailyGoal>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
