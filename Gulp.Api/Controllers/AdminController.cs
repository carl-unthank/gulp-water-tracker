using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Models;
using Gulp.Shared.Interfaces;

namespace Gulp.Api.Controllers;

/// <summary>
/// Admin-only endpoints for user management and system administration
/// </summary>
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IRepository<User> userRepository,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// GET /api/admin/users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Email!.Contains(search) ||
                                        u.FirstName!.Contains(search) ||
                                        u.LastName!.Contains(search));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<AdminUserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var customUser = (await _userRepository.FindAsync(u => u.AspNetUserId == user.Id)).FirstOrDefault();

                userDtos.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    IsDeleted = user.IsDeleted,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = roles.ToList(),
                    CustomUserId = customUser?.Id
                });
            }

            var result = new PagedResult<AdminUserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting users");
        }
    }

    /// <summary>
    /// Get user by ID
    /// GET /api/admin/users/{id}
    /// </summary>
    [HttpGet("users/{id:int}")]
    public async Task<ActionResult<AdminUserDto>> GetUser(int id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userDto = await MapToAdminUserDtoAsync(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting user", id);
        }
    }

    /// <summary>
    /// Update user
    /// PUT /api/admin/users/{id}
    /// </summary>
    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(int id, [FromBody] AdminUpdateUserDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update user properties
            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Email = updateDto.Email;
            user.UserName = updateDto.Email;
            user.EmailConfirmed = updateDto.EmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });
            }

            // Update roles if provided
            if (updateDto.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, updateDto.Roles);
            }

            _logger.LogInformation("User {UserId} updated by admin {AdminId}", id, GetRequiredUserId());

            // Return updated user
            return await GetUser(id);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error updating user", id);
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// DELETE /api/admin/users/{id}
    /// </summary>
    [HttpDelete("users/{id:int}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            var adminUserId = GetRequiredUserId();
            
            // Prevent admin from deleting themselves
            if (id == adminUserId)
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Soft delete
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to delete user", errors = result.Errors });
            }

            _logger.LogInformation("User {UserId} deleted by admin {AdminId}", id, adminUserId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error deleting user", id);
        }
    }

    /// <summary>
    /// Lock/unlock user account
    /// POST /api/admin/users/{id}/lock
    /// </summary>
    [HttpPost("users/{id:int}/lock")]
    public async Task<ActionResult> LockUser(int id, [FromBody] LockUserDto lockDto)
    {
        try
        {
            var adminUserId = GetRequiredUserId();
            
            // Prevent admin from locking themselves
            if (id == adminUserId)
            {
                return BadRequest(new { message = "Cannot lock your own account" });
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var lockoutEnd = lockDto.Lock ? DateTimeOffset.UtcNow.AddYears(100) : (DateTimeOffset?)null;
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update user lockout status", errors = result.Errors });
            }

            var action = lockDto.Lock ? "locked" : "unlocked";
            _logger.LogInformation("User {UserId} {Action} by admin {AdminId}", id, action, adminUserId);

            return Ok(new { message = $"User {action} successfully" });
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error updating user lockout status", id);
        }
    }

    /// <summary>
    /// Get system statistics
    /// GET /api/admin/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetSystemStats()
    {
        try
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.Where(u => !u.IsDeleted).CountAsync();
            var lockedUsers = await _userManager.Users.Where(u => u.LockoutEnd > DateTimeOffset.UtcNow).CountAsync();
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            var stats = new AdminStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                LockedUsers = lockedUsers,
                AdminUsers = adminUsers.Count,
                DeletedUsers = totalUsers - activeUsers
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return HandleInternalError(_logger, ex, "Error getting system stats");
        }
    }

    /// <summary>
    /// Convert ApplicationUser to AdminUserDto with roles and custom user data
    /// </summary>
    private async Task<AdminUserDto> MapToAdminUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var customUser = (await _userRepository.FindAsync(u => u.AspNetUserId == user.Id)).FirstOrDefault();

        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnd = user.LockoutEnd,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles.ToList(),
            CustomUserId = customUser?.Id
        };
    }
}
