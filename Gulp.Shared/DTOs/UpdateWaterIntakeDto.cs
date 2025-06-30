using System.ComponentModel.DataAnnotations;

namespace Gulp.Shared.DTOs;

public class UpdateWaterIntakeDto
{
    [Required]
    [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10000 ml")]
    public int AmountMl { get; set; }
    
    [Required]
    public DateTime ConsumedAt { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
