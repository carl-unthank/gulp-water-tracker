using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// Water intake history and analytics
/// </summary>
[ApiController]
[Route("api/history")]
[Authorize]
public class HistoryController : ControllerBase
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
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Default to last 30 days if no dates provided
        var start = startDate?.Date ?? DateTime.Today.AddDays(-30);
        var end = endDate?.Date ?? DateTime.Today;

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 30;

        var result = await _waterIntakeService.GetWaterIntakeHistoryAsync(
            userId.Value, 
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
                    _logger.LogError(exception, "Error getting water intake history for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
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
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.GetUserStatsAsync(userId.Value);
        
        return result.Match(
            onSuccess: stats => Ok(stats),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting user stats for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
            }
        );
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
