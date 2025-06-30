using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Models;
using Gulp.Shared.Common;

namespace Gulp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IRepository<User> userRepository,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return Result<AuthResponseDto>.Failure("User with this email already exists.", "USER_EXISTS");
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                EmailConfirmed = true // For simplicity, auto-confirm emails
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return Result<AuthResponseDto>.Failure(
                    string.Join(", ", result.Errors.Select(e => e.Description)),
                    "REGISTRATION_FAILED"
                );
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            // Create corresponding User record in custom User table
            var customUser = new Gulp.Shared.Models.User
            {
                AspNetUserId = user.Id
            };
            await _userRepository.AddAsync(customUser);

            // Create default daily goal using the custom User ID
            var defaultGoal = new DailyGoal
            {
                UserId = customUser.Id,
                TargetAmountMl = 2000, // Default 2L goal
                EffectiveDate = DateTime.Today
            };
            // TODO: Fix with new repository pattern - for now, this will be handled elsewhere
            // await _unitOfWork.DailyGoals.AddAsync(defaultGoal);

            // TODO: Implement audit logging service
            // await _unitOfWork.AuditLogs.LogActionAsync(customUser.Id, "USER_REGISTERED", "User account created");
            // await _unitOfWork.SaveChangesAsync();

            // Generate token pair with custom user ID
            var tokenPair = await _jwtService.GenerateTokenPairAsync(user, customUser.Id);

            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful",
                User = MapToUserDto(user),
                Token = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                TokenExpiry = tokenPair.AccessTokenExpiry,
                RefreshTokenExpiry = tokenPair.RefreshTokenExpiry
            };

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
            return Result<AuthResponseDto>.Failure(
                ex,
                "An error occurred during registration. Please try again."
            );
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

        try
        {
            // Validate user exists and is not deleted
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || user.IsDeleted)
            {
                _logger.LogWarning("Login failed for email {Email}: User not found or deleted", loginDto.Email);
                return Result<AuthResponseDto>.Failure("Invalid email or password", "USER_NOT_FOUND");
            }

            // Validate password
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                var (errorMessage, errorCode) = result switch
                {
                    { IsLockedOut: true } => ("Account is temporarily locked due to multiple failed attempts", "ACCOUNT_LOCKED"),
                    { IsNotAllowed: true } => ("Account is not allowed to sign in", "ACCOUNT_NOT_ALLOWED"),
                    _ => ("Invalid email or password", "INVALID_PASSWORD")
                };

                _logger.LogWarning("Login failed for user {Email} (ID: {UserId}): {ErrorCode}",
                    loginDto.Email, user.Id, errorCode);

                // TODO: Implement audit logging service
                // await _unitOfWork.AuditLogs.LogActionAsync(user.Id, "LOGIN_FAILED", errorCode);

                return Result<AuthResponseDto>.Failure(errorMessage, errorCode);
            }

            // Find the custom user record
            var customUser = await _userRepository.FindAsync(u => u.AspNetUserId == user.Id);
            var customUserId = customUser?.FirstOrDefault()?.Id;

            // Generate token pair with custom user ID
            var tokenPair = await _jwtService.GenerateTokenPairAsync(user, customUserId);

            // TODO: Implement audit logging service
            // await _unitOfWork.AuditLogs.LogActionAsync(user.Id, "USER_LOGIN", "User logged in successfully");

            _logger.LogInformation("User {Email} (ID: {UserId}) logged in successfully", loginDto.Email, user.Id);

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                User = MapToUserDto(user),
                Token = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                TokenExpiry = tokenPair.AccessTokenExpiry,
                RefreshTokenExpiry = tokenPair.RefreshTokenExpiry
            };

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", loginDto.Email);
            return Result<AuthResponseDto>.Failure(
                ex,
                "An unexpected error occurred during login. Please try again."
            );
        }
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenPair = await _jwtService.RefreshTokenPairAsync(refreshToken);
            if (tokenPair == null)
            {
                return Result<AuthResponseDto>.Failure("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");
            }

            // Get user ID from the new access token to return user info
            var userIdString = _jwtService.GetUserIdFromToken(tokenPair.AccessToken);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Result<AuthResponseDto>.Failure("Invalid token.", "INVALID_TOKEN");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result<AuthResponseDto>.Failure("User not found.", "USER_NOT_FOUND");
            }

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Token refreshed successfully",
                User = MapToUserDto(user),
                Token = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                TokenExpiry = tokenPair.AccessTokenExpiry,
                RefreshTokenExpiry = tokenPair.RefreshTokenExpiry
            };

            return Result<AuthResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<AuthResponseDto>.Failure(
                ex,
                "An error occurred during token refresh."
            );
        }
    }

    public async Task<Result> LogoutAsync(int userId, string? refreshToken = null)
    {
        try
        {
            // Revoke refresh token if provided
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _jwtService.RevokeRefreshTokenAsync(refreshToken);
            }

            // TODO: Implement audit logging service
            // await _unitOfWork.AuditLogs.LogActionAsync(userId, "USER_LOGOUT", "User logged out");
            // await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} logged out", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return Result.Failure(ex, "An error occurred during logout");
        }
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(int userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result<UserDto>.Failure("User not found", "USER_NOT_FOUND");
            }

            var userDto = MapToUserDto(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user {UserId}", userId);
            return Result<UserDto>.Failure(ex, "An error occurred while retrieving user information");
        }
    }

    public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
            {
                return Result.Failure("User not found", "USER_NOT_FOUND");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                // TODO: Implement audit logging service
                // await _unitOfWork.AuditLogs.LogActionAsync(userId, "PASSWORD_CHANGED", "User changed password");
                // await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User {UserId} changed password", userId);
                return Result.Success();
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors, "PASSWORD_CHANGE_FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result.Failure(ex, "An error occurred while changing password");
        }
    }

    public async Task<Result> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.IsDeleted)
            {
                // Don't reveal if user exists or not - return success for security
                return Result.Success();
            }

            // In a real application, you would send an email with a reset token
            // For now, we'll just log the action
            // TODO: Implement audit logging service
            // await _unitOfWork.AuditLogs.LogActionAsync(user.Id, "PASSWORD_RESET_REQUESTED", "Password reset requested");
            // await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset requested for {Email}", email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", email);
            return Result.Failure(ex, "An error occurred during password reset");
        }
    }

    private static UserDto MapToUserDto(ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        };
    }
}
