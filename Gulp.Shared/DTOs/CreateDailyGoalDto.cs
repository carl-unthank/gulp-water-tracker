using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.DTOs;

public class CreateDailyGoalDto
{
    [Required]
    [Range(500, 10000, ErrorMessage = "Daily goal must be between 500 and 10000 ml")]
    public int TargetAmountMl { get; set; }
}
