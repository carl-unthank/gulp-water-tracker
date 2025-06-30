namespace Gulp.Shared.DTOs;

public class HistoryDto
{
    public DateTime Date { get; set; }
    public int TotalMl { get; set; }
    public int GoalMl { get; set; }
    public double ProgressPercentage { get; set; }
    public bool GoalAchieved { get; set; }
    public List<WaterIntakeDto> Intakes { get; set; } = new();
}
