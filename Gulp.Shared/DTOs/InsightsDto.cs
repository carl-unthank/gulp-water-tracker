namespace Gulp.Shared.DTOs;

public class InsightsDto
{
    public double AverageDaily7Days { get; set; }
    public double AverageDaily30Days { get; set; }
    public int TotalWeekMl { get; set; }
    public int TotalMonthMl { get; set; }
    public int CompletedDaysThisWeek { get; set; }
    public int CompletedDaysThisMonth { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
}
