using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// RESTful user management
/// </summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
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
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
