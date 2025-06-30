using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;

namespace Gulp.Api.Controllers;

/// <summary>
/// RESTful session management (login/logout)
/// </summary>
[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        IAuthService authService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<SessionsController> logger)
    {
        _authService = authService;
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a new session (login)
    /// POST /api/sessions
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AuthResponseDto>> CreateSession([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed for email {Email}: User not found", loginDto.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check password and sign in
            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

                // Create response DTO
                var response = new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = $"{user.FirstName} {user.LastName}".Trim()
                    }
                    // No Token needed - using cookies
                };

                return Ok(response);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed for email {Email}: Account locked", loginDto.Email);
                return Unauthorized(new { message = "Account is locked due to multiple failed attempts" });
            }

            _logger.LogWarning("Login failed for email {Email}: Invalid credentials", loginDto.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Delete current session (logout)
    /// DELETE /api/sessions/current
    /// </summary>
    [HttpDelete("current")]
    [Authorize]
    public async Task<ActionResult> DeleteCurrentSession()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out successfully");
            return NoContent(); // 204 No Content - standard for successful DELETE
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Get current session status
    /// GET /api/sessions/current
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> GetCurrentSession()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user != null)
                {
                    var response = new AuthResponseDto
                    {
                        Success = true,
                        Message = "Session active",
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            FullName = $"{user.FirstName} {user.LastName}".Trim()
                        }
                    };

                    return Ok(response);
                }
            }

            return Unauthorized(new { message = "Invalid session" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current session");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
