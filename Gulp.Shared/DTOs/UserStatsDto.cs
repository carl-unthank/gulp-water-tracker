namespace Gulp.Shared.DTOs;

/// <summary>
/// User statistics for water intake tracking
/// </summary>
public class UserStatsDto
{
    public int TotalDaysTracked { get; set; }
    public int GoalsAchieved { get; set; }
    public decimal OverallAchievementPercentage { get; set; }
    public int TotalWaterIntakeMl { get; set; }
    public int AverageIntakeMl { get; set; }
    public int BestStreakDays { get; set; }
    public int CurrentStreakDays { get; set; }
    public DateOnly? LastIntakeDate { get; set; }
    public int BestDayIntakeMl { get; set; }
    public DateOnly? BestDay { get; set; }
}
