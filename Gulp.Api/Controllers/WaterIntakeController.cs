using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Common;

namespace Gulp.Api.Controllers;

/// <summary>
/// Water intake tracking endpoints
/// </summary>
[ApiController]
[Route("api/intakes")]
[Authorize]
public class WaterIntakeController : ControllerBase
{
    private readonly IWaterIntakeService _waterIntakeService;
    private readonly ILogger<WaterIntakeController> _logger;

    public WaterIntakeController(
        IWaterIntakeService waterIntakeService,
        ILogger<WaterIntakeController> logger)
    {
        _waterIntakeService = waterIntakeService;
        _logger = logger;
    }

    /// <summary>
    /// Record water intake
    /// POST /api/water-intake
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WaterIntakeDto>> RecordWaterIntake([FromBody] CreateWaterIntakeDto createDto)
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

        var result = await _waterIntakeService.RecordWaterIntakeAsync(userId.Value, createDto);
        
        if (result.IsSuccess && result.Value is not null)
        {
            _logger.LogInformation("Water intake recorded for user {UserId}: {Amount}ml", userId, createDto.AmountMl);
            return CreatedAtAction(nameof(GetWaterIntakeById), new { id = result.Value.Id }, result.Value);
        }

        if (result.Exception != null)
        {
            _logger.LogError(result.Exception, "Error recording water intake for user {UserId}", userId);
            return StatusCode(500, new { message = "An internal server error occurred" });
        }

        _logger.LogWarning("Failed to record water intake for user {UserId}: {ErrorCode} - {Message}",
            userId, result.ErrorCode, result.ErrorMessage);

        return BadRequest(new { message = result.ErrorMessage });
    }

    /// <summary>
    /// Get water intake by ID
    /// GET /api/water-intake/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WaterIntakeDto>> GetWaterIntakeById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.GetWaterIntakeByIdAsync(id, userId.Value);
        
        return result.Match(
            onSuccess: waterIntake => Ok(waterIntake),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting water intake {Id} for user {UserId}", id, userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return errorCode switch
                {
                    "NOT_FOUND" => NotFound(new { message = errorMessage }),
                    "UNAUTHORIZED" => StatusCode(403, new { message = errorMessage }),
                    _ => BadRequest(new { message = errorMessage })
                };
            }
        );
    }

    /// <summary>
    /// Get today's water intake for current user
    /// GET /api/water-intake/today
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<WaterIntakeDto>>> GetTodaysWaterIntake()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.GetTodaysWaterIntakeAsync(userId.Value);
        
        return result.Match(
            onSuccess: waterIntakes => Ok(waterIntakes),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting today's water intake for user {UserId}", userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
            }
        );
    }

    /// <summary>
    /// Get water intake for a specific date
    /// GET /api/water-intake/date/{date}
    /// </summary>
    [HttpGet("date/{date:datetime}")]
    public async Task<ActionResult<IEnumerable<WaterIntakeDto>>> GetWaterIntakeByDate(DateTime date)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.GetWaterIntakeByDateAsync(userId.Value, DateOnly.FromDateTime(date));
        
        return result.Match(
            onSuccess: waterIntakes => Ok(waterIntakes),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting water intake for date {Date} for user {UserId}", date, userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return BadRequest(new { message = errorMessage });
            }
        );
    }

    /// <summary>
    /// Update water intake entry
    /// PUT /api/water-intake/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WaterIntakeDto>> UpdateWaterIntake(int id, [FromBody] UpdateWaterIntakeDto updateDto)
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

        var result = await _waterIntakeService.UpdateWaterIntakeAsync(id, userId.Value, updateDto);
        
        return result.Match(
            onSuccess: waterIntake => 
            {
                _logger.LogInformation("Water intake {Id} updated for user {UserId}", id, userId);
                return Ok(waterIntake);
            },
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error updating water intake {Id} for user {UserId}", id, userId);
                    return StatusCode(500, new { message = "An internal server error occurred" });
                }

                return errorCode switch
                {
                    "NOT_FOUND" => NotFound(new { message = errorMessage }),
                    "UNAUTHORIZED" => StatusCode(403, new { message = errorMessage }),
                    _ => BadRequest(new { message = errorMessage })
                };
            }
        );
    }

    /// <summary>
    /// Delete water intake entry
    /// DELETE /api/water-intake/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteWaterIntake(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.DeleteWaterIntakeAsync(id, userId.Value);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Water intake {Id} deleted for user {UserId}", id, userId);
            return NoContent();
        }

        if (result.Exception is not null)
        {
            _logger.LogError(result.Exception, "Error deleting water intake {Id} for user {UserId}", id, userId);
            return StatusCode(500, new { message = "An internal server error occurred" });
        }

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(new { message = result.ErrorMessage }),
            "UNAUTHORIZED" => StatusCode(403, new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    /// <summary>
    /// Get current user's daily progress
    /// GET /api/water-intake/progress
    /// </summary>
    [HttpGet("progress")]
    public async Task<ActionResult<HistoryDto>> GetDailyProgress()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _waterIntakeService.GetDailyProgressAsync(userId.Value);
        
        return result.Match(
            onSuccess: progress => Ok(progress),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    _logger.LogError(exception, "Error getting daily progress for user {UserId}", userId);
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
