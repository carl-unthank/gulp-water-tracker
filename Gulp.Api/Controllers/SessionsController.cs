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

                // Get user roles for UI convenience (AuthorizeView components)
                var roles = await _userManager.GetRolesAsync(user);

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
                    },
                    Roles = roles.ToList()
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
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid session" });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid session" });
            }

            // Get user roles for client-side role checking
            var roles = await _userManager.GetRolesAsync(user);

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
                },
                Roles = roles.ToList() // For UI convenience - server still validates with [Authorize]
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current session");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Check if current user is admin - for routing decisions only
    /// This is secure because it uses server-side role verification from cookie claims
    /// </summary>
    [HttpGet("is-admin")]
    [Authorize]
    public ActionResult<bool> IsAdmin()
    {
        try
        {
            // Read role directly from authenticated cookie claims - secure and efficient
            var isAdmin = User.IsInRole("Admin");
            return Ok(isAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin status");
            return StatusCode(500, false);
        }
    }

    /// <summary>
    /// Get minimal user info for client-side UI updates
    /// Only exposes non-sensitive claims needed for UI personalization
    /// Roles are NOT exposed here - use is-admin endpoint for role checks
    /// </summary>
    [HttpGet("user-info")]
    [Authorize]
    public ActionResult<object> GetUserInfo()
    {
        try
        {
            // Only expose minimal, non-sensitive claims for UI purposes
            var userInfo = new
            {
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? "User",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                FirstName = User.FindFirst("FirstName")?.Value ?? "",
                LastName = User.FindFirst("LastName")?.Value ?? ""
                // Deliberately NOT exposing roles here - use is-admin endpoint instead
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
