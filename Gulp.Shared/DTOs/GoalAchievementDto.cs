namespace Gulp.Shared.DTOs;

public class GoalAchievementDto
{
    public DateTime Date { get; set; }
    public int GoalMl { get; set; }
    public int ActualMl { get; set; }
    public bool Achieved { get; set; }
    public double ProgressPercentage { get; set; }
}
