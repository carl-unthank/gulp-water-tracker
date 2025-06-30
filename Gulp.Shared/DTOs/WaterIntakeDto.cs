namespace Gulp.Shared.DTOs;

public class WaterIntakeDto
{
    public int Id { get; set; }
    public int AmountMl { get; set; }
    public DateTime ConsumedAt { get; set; }
    public string? Notes { get; set; }
}
