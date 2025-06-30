using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Gulp.Infrastructure.Services;
using Gulp.Infrastructure.Data;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;

namespace Gulp.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        // Mock UserManager
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);

        _mockJwtService = new Mock<IJwtService>();
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _service = new AuthService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockJwtService.Object,
            _mockUserRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = loginDto.Email,
            UserName = loginDto.Email,
            FirstName = "Test",
            LastName = "User",
            IsDeleted = false
        };

        var tokenPair = new TokenPair(
            "access-token",
            "refresh-token",
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow.AddDays(7)
        );

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, true))
            .ReturnsAsync(SignInResult.Success);

        _mockJwtService
            .Setup(js => js.GenerateTokenPairAsync(user, It.IsAny<int?>()))
            .ReturnsAsync(tokenPair);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Login successful", result.Value.Message);
        Assert.Equal(tokenPair.AccessToken, result.Value.Token);
        Assert.Equal(tokenPair.RefreshToken, result.Value.RefreshToken);
        Assert.NotNull(result.Value.User);
        Assert.Equal(user.Email, result.Value.User.Email);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123!"
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        Assert.Equal("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_DeletedUser_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "deleted@example.com",
            Password = "TestPassword123!"
        };

        var deletedUser = new ApplicationUser
        {
            Id = 1,
            Email = loginDto.Email,
            UserName = loginDto.Email,
            IsDeleted = true
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(deletedUser);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        Assert.Equal("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = loginDto.Email,
            UserName = loginDto.Email,
            IsDeleted = false
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_PASSWORD", result.ErrorCode);
        Assert.Contains("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
            FirstName = "New",
            LastName = "User"
        };

        var tokenPair = new TokenPair(
            "access-token",
            "refresh-token",
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow.AddDays(7)
        );

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) => user.Id = 1);

        _mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        _mockJwtService
            .Setup(js => js.GenerateTokenPairAsync(It.IsAny<ApplicationUser>(), It.IsAny<int?>()))
            .ReturnsAsync(tokenPair);

        // Act
        var result = await _service.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Success);
        Assert.Equal("Registration successful", result.Value.Message);
        Assert.Equal(tokenPair.AccessToken, result.Value.Token);
        Assert.Equal(tokenPair.RefreshToken, result.Value.RefreshToken);
        Assert.NotNull(result.Value.User);
        Assert.Equal(registerDto.Email, result.Value.User.Email);
        Assert.Equal(registerDto.FirstName, result.Value.User.FirstName);
        Assert.Equal(registerDto.LastName, result.Value.User.LastName);

        // Verify the service creates the user with correct properties
        _mockUserManager.Verify(um => um.CreateAsync(It.Is<ApplicationUser>(u =>
            u.Email == registerDto.Email &&
            u.UserName == registerDto.Email &&
            u.FirstName == registerDto.FirstName &&
            u.LastName == registerDto.LastName), registerDto.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFailureResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var existingUser = new ApplicationUser
        {
            Id = 1,
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        _mockUserManager
            .Setup(um => um.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_EXISTS", result.ErrorCode);
        Assert.Equal("User with this email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ValidUser_ReturnsSuccessResult()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsDeleted = false
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal(user.FirstName, result.Value.FirstName);
        Assert.Equal(user.LastName, result.Value.LastName);
    }

    [Fact]
    public async Task GetCurrentUserAsync_DeletedUser_ReturnsFailureResult()
    {
        // Arrange
        var userId = 1;
        var deletedUser = new ApplicationUser
        {
            Id = userId,
            Email = "deleted@example.com",
            IsDeleted = true
        };

        _mockUserManager
            .Setup(um => um.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(deletedUser);

        // Act
        var result = await _service.GetCurrentUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        Assert.Equal("User not found", result.ErrorMessage);
    }

    [Fact]
    public async Task LogoutAsync_ValidUser_ReturnsSuccessResult()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _service.LogoutAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
