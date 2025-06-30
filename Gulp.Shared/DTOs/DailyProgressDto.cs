namespace Gulp.Shared.DTOs;

public class DailyProgressDto
{
    public DateTime Date { get; set; }
    public int TotalIntake { get; set; }
    public int DailyGoal { get; set; }
    public bool GoalAchieved { get; set; }
    public double ProgressPercentage { get; set; }
}
