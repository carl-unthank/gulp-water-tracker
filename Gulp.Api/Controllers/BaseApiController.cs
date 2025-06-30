using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gulp.Api.Controllers;

/// <summary>
/// Base controller for all authenticated API endpoints
/// Provides common functionality like user ID extraction
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current authenticated user's ID, throwing an exception if not found
    /// Use this when you're certain the user should be authenticated (after [Authorize])
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID cannot be extracted</exception>
    protected int GetRequiredUserId()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gets the current authenticated user's email from JWT claims
    /// </summary>
    /// <returns>User email if found, null otherwise</returns>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="role">Role name to check</param>
    /// <returns>True if user has the role, false otherwise</returns>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }

    /// <summary>
    /// Creates a standardized error response for internal server errors
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="message">User-friendly error message</param>
    /// <param name="userId">User ID for logging context</param>
    /// <returns>500 Internal Server Error response</returns>
    protected ActionResult HandleInternalError(ILogger logger, Exception exception, string message, int? userId = null)
    {
        if (userId.HasValue)
        {
            logger.LogError(exception, "{Message} for user {UserId}", message, userId.Value);
        }
        else
        {
            logger.LogError(exception, message);
        }
        
        return StatusCode(500, new { message = "An internal server error occurred" });
    }

    /// <summary>
    /// Creates a standardized error response based on Result pattern error codes
    /// </summary>
    /// <param name="errorMessage">Error message from the Result</param>
    /// <param name="errorCode">Error code from the Result</param>
    /// <returns>Appropriate HTTP response based on error code</returns>
    protected ActionResult HandleResultError(string errorMessage, string? errorCode)
    {
        return errorCode switch
        {
            "NOT_FOUND" or "USER_NOT_FOUND" => NotFound(new { message = errorMessage }),
            "UNAUTHORIZED" => Unauthorized(new { message = errorMessage }),
            "FORBIDDEN" => Forbid(),
            "VALIDATION_ERROR" => BadRequest(new { message = errorMessage }),
            _ => BadRequest(new { message = errorMessage })
        };
    }
}
