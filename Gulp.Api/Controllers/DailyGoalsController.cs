using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// Daily water intake goals management
/// </summary>
[ApiController]
[Route("api/goals")]
[Authorize]
public class DailyGoalsController : ControllerBase
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
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dailyGoalService.GetCurrentDailyGoalAsync(userId.Value);
        
        return result.Match(
            onSuccess: goal => Ok(goal),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting current daily goal for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return errorCode switch
                {
                    "NOT_FOUND" => NotFound(new { message = errorMessage }),
                    _ => BadRequest(new { message = errorMessage })
                };
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

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Convert CreateDailyGoalDto to DailyGoalDto for the service
        var updateDto = new DailyGoalDto
        {
            TargetAmountMl = createDto.TargetAmountMl
        };

        var result = await _dailyGoalService.UpdateDailyGoalAsync(userId.Value, updateDto);

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
                    _logger.LogError(exception, "Error creating daily goal for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
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

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _dailyGoalService.UpdateDailyGoalAsync(userId.Value, updateDto);
        
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
                    _logger.LogError(exception, "Error updating daily goal for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
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
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _dailyGoalService.GetDailyGoalHistoryAsync(userId.Value, page, pageSize);
        
        return result.Match(
            onSuccess: goals => Ok(goals),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting daily goal history for user {UserId}", userId);
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
}
