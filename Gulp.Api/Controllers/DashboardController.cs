using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// Dashboard and analytics endpoints
/// </summary>
[Route("api/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IWaterIntakeService _waterIntakeService;
    private readonly IDailyGoalService _dailyGoalService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IWaterIntakeService waterIntakeService,
        IDailyGoalService dailyGoalService,
        ILogger<DashboardController> logger)
    {
        _waterIntakeService = waterIntakeService;
        _dailyGoalService = dailyGoalService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard data for current user
    /// GET /api/dashboard
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var userId = GetRequiredUserId();

        try
        {
            // Get current goal
            var goalResult = await _dailyGoalService.GetCurrentDailyGoalAsync(userId);
            var currentGoal = goalResult.IsSuccess ? goalResult.Value : null;

            // Get today's intakes
            var intakesResult = await _waterIntakeService.GetTodaysWaterIntakeAsync(userId);
            var todayIntakes = intakesResult.IsSuccess && intakesResult.Value != null ? intakesResult.Value.ToList() : new List<WaterIntakeDto>();

            // Calculate progress
            var totalIntake = todayIntakes.Sum(i => i.AmountMl);
            var targetAmount = currentGoal?.TargetAmountMl ?? 2000;
            var progressPercentage = targetAmount > 0 ? (double)totalIntake / targetAmount * 100 : 0;

            // Get insights (simplified for now)
            var insights = new InsightsDto
            {
                AverageDaily7Days = totalIntake,
                AverageDaily30Days = totalIntake,
                TotalWeekMl = totalIntake * 7, 
                TotalMonthMl = totalIntake * 30, 
                CompletedDaysThisWeek = totalIntake >= targetAmount ? 1 : 0,
                CompletedDaysThisMonth = totalIntake >= targetAmount ? 1 : 0,
                CurrentStreak = totalIntake >= targetAmount ? 1 : 0,
                LongestStreak = 1 
            };

            var dashboard = new DashboardDto
            {
                User = new UserDto { Id = userId },
                CurrentGoal = currentGoal,
                TodayIntakeMl = totalIntake,
                TodayProgressPercentage = progressPercentage,
                TodayIntakes = todayIntakes,
                Insights = insights
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting dashboard", userId);
        }
    }

    /// <summary>
    /// Get insights for current user
    /// GET /api/dashboard/insights
    /// </summary>
    [HttpGet("insights")]
    public async Task<ActionResult<InsightsDto>> GetInsights()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            // For now, return simplified insights
            // In a real implementation, this would calculate actual statistics
            var insights = new InsightsDto
            {
                AverageDaily7Days = 1800,
                AverageDaily30Days = 1750,
                TotalWeekMl = 12600,
                TotalMonthMl = 52500,
                CompletedDaysThisWeek = 5,
                CompletedDaysThisMonth = 22,
                CurrentStreak = 3,
                LongestStreak = 12
            };

            return Ok(insights);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting insights", userId);
        }
    }

    /// <summary>
    /// Get history data for current user
    /// GET /api/dashboard/history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<HistoryDto>>> GetHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetRequiredUserId();

        // Default to last 30 days if no dates provided
        var start = startDate?.Date ?? DateTime.Today.AddDays(-30);
        var end = endDate?.Date ?? DateTime.Today;

        var result = await _waterIntakeService.GetWaterIntakeHistoryAsync(
            userId,
            DateOnly.FromDateTime(start),
            DateOnly.FromDateTime(end),
            1,
            100);

        return result.Match(
            onSuccess: history => Ok(history),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting history", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }

    /// <summary>
    /// Get daily progress data for current user
    /// GET /api/dashboard/daily-progress
    /// </summary>
    [HttpGet("daily-progress")]
    public async Task<ActionResult<List<DailyProgressDto>>> GetDailyProgress(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = GetRequiredUserId();

        // Default to last 7 days if no dates provided
        var start = startDate?.Date ?? DateTime.Today.AddDays(-6);
        var end = endDate?.Date ?? DateTime.Today;

        try
        {
            var progressList = new List<DailyProgressDto>();

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                // Get the goal that was effective for this specific date
                var goalResult = await _dailyGoalService.GetDailyGoalForDateAsync(userId, date);
                var dailyGoal = goalResult.IsSuccess ? goalResult.Value.TargetAmountMl : 2000;

                // Get daily intakes for this date
                var intakesResult = await _waterIntakeService.GetWaterIntakeByDateAsync(userId, DateOnly.FromDateTime(date));
                var dailyTotal = 0;

                if (intakesResult.IsSuccess)
                {
                    dailyTotal = intakesResult.Value.Sum(i => i.AmountMl);
                }

                var progress = new DailyProgressDto
                {
                    Date = date,
                    TotalIntake = dailyTotal,
                    DailyGoal = dailyGoal,
                    GoalAchieved = dailyTotal >= dailyGoal,
                    ProgressPercentage = dailyGoal > 0 ? (double)dailyTotal / dailyGoal * 100 : 0
                };

                progressList.Add(progress);
            }

            return Ok(progressList);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting daily progress", userId);
        }
    }


}
