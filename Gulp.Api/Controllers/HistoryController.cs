using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// Water intake history and analytics
/// </summary>
[Route("api/history")]
public class HistoryController : BaseApiController
{
    private readonly IWaterIntakeService _waterIntakeService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        IWaterIntakeService waterIntakeService,
        ILogger<HistoryController> logger)
    {
        _waterIntakeService = waterIntakeService;
        _logger = logger;
    }

    /// <summary>
    /// Get water intake history for current user
    /// GET /api/history
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HistoryDto>>> GetWaterIntakeHistory(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var userId = GetRequiredUserId();

        // Default to last 30 days if no dates provided
        var start = startDate?.Date ?? DateTime.Today.AddDays(-30);
        var end = endDate?.Date ?? DateTime.Today;

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 30;

        var result = await _waterIntakeService.GetWaterIntakeHistoryAsync(
            userId,
            DateOnly.FromDateTime(start),
            DateOnly.FromDateTime(end),
            page,
            pageSize);
        
        return result.Match(
            onSuccess: history => Ok(history),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting water intake history", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }



    /// <summary>
    /// Get user statistics
    /// GET /api/history/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> GetUserStats()
    {
        var userId = GetRequiredUserId();
        var result = await _waterIntakeService.GetUserStatsAsync(userId);
        
        return result.Match(
            onSuccess: stats => Ok(stats),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting user stats", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }



    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
