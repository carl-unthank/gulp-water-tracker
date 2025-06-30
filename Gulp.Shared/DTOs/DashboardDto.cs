namespace Gulp.Shared.DTOs;

public class DashboardDto
{
    public UserDto User { get; set; } = null!;
    public DailyGoalDto? CurrentGoal { get; set; }
    public int TodayIntakeMl { get; set; }
    public double TodayProgressPercentage { get; set; }
    public List<WaterIntakeDto> TodayIntakes { get; set; } = new();
    public InsightsDto Insights { get; set; } = null!;
}
