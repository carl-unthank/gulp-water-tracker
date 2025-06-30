using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.Models;

public class DailyGoal : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [Range(500, 10000, ErrorMessage = "Daily goal must be between 500 and 10000 ml")]
    public int TargetAmountMl { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
