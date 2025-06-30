using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// RESTful user management
/// </summary>
[Route("api/users")]
public class UsersController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IAuthService authService, 
        ILogger<UsersController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user (registration)
    /// POST /api/users
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AuthResponseDto>> CreateUser([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(registerDto);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);
            
            // Set HTTP-only cookie for JWT
            if (!string.IsNullOrEmpty(result.Value?.Token))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // HTTPS only
                    SameSite = SameSiteMode.Strict,
                    Expires = result.Value.TokenExpiry
                };
                Response.Cookies.Append("access_token", result.Value.Token, cookieOptions);
            }

            return Created("", result.Value);
        }

        _logger.LogWarning("Registration failed for {Email}: {Error}", registerDto.Email, result.ErrorMessage);
        return BadRequest(new { message = result.ErrorMessage });
    }

    /// <summary>
    /// Get current user information
    /// GET /api/users/current
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetRequiredUserId();
        var result = await _authService.GetCurrentUserAsync(userId);

        return result.Match(
            onSuccess: user => Ok(user),
            onFailure: (errorMessage, errorCode, exception) =>
            {
                if (exception != null)
                {
                    return HandleInternalError(_logger, exception, "Error getting current user", userId);
                }

                return HandleResultError(errorMessage, errorCode);
            }
        );
    }
}
