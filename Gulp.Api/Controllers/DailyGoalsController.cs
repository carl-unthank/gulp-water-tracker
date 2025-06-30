using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// Daily water intake goals management
/// </summary>
[Route("api/goals")]
public class DailyGoalsController : BaseApiController
{
    private readonly IDailyGoalService _dailyGoalService;
    private readonly ILogger<DailyGoalsController> _logger;

    public DailyGoalsController(
        IDailyGoalService dailyGoalService,
        ILogger<DailyGoalsController> logger)
    {
        _dailyGoalService = dailyGoalService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's daily goal
    /// GET /api/daily-goals/current
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<DailyGoalDto>> GetCurrentDailyGoal()
    {
        var userId = GetRequiredUserId();
        var result = await _dailyGoalService.GetCurrentDailyGoalAsync(userId);
        
        return result.Match(
            onSuccess: goal => Ok(goal),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting current daily goal", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }

    /// <summary>
    /// Create a new daily goal for current user
    /// POST /api/goals
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DailyGoalDto>> CreateDailyGoal([FromBody] DailyGoalDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetRequiredUserId();

        // Convert CreateDailyGoalDto to DailyGoalDto for the service
        var updateDto = new DailyGoalDto
        {
            TargetAmountMl = createDto.TargetAmountMl
        };

        var result = await _dailyGoalService.UpdateDailyGoalAsync(userId, updateDto);

        return result.Match(
            onSuccess: goal =>
            {
                _logger.LogInformation("Daily goal created for user {UserId}: {TargetAmountMl}ml", userId, createDto.TargetAmountMl);
                return CreatedAtAction(nameof(GetCurrentDailyGoal), new { }, goal);
            },
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error creating daily goal", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }

    /// <summary>
    /// Update current user's daily goal
    /// PUT /api/goals/current
    /// </summary>
    [HttpPut("current")]
    public async Task<ActionResult<DailyGoalDto>> UpdateCurrentDailyGoal([FromBody] DailyGoalDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetRequiredUserId();
        var result = await _dailyGoalService.UpdateDailyGoalAsync(userId, updateDto);
        
        return result.Match(
            onSuccess: goal => 
            {
                _logger.LogInformation("Daily goal updated for user {UserId}: {TargetAmountMl}ml", userId, updateDto.TargetAmountMl);
                return Ok(goal);
            },
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error updating daily goal", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }

    /// <summary>
    /// Get daily goal history for current user
    /// GET /api/daily-goals/history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<DailyGoalDto>>> GetDailyGoalHistory(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var userId = GetRequiredUserId();

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _dailyGoalService.GetDailyGoalHistoryAsync(userId, page, pageSize);

        return result.Match(
            onSuccess: goals => Ok(goals),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting daily goal history", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }
}
