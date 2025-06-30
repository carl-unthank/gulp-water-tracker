using Gulp.Shared.Models;
using Gulp.Infrastructure.Data;

namespace Gulp.Tests.Unit;

/// <summary>
/// Tests for User domain model and related business logic
/// Follows Single Responsibility Principle - only tests User-related functionality
/// </summary>
public class UserTests
{
    #region Test Data Factory Methods (Private - Single Responsibility)

    private static User CreateTestUser(int id = 1, int aspNetUserId = 1)
    {
        return new User
        {
            Id = id,
            AspNetUserId = aspNetUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static ApplicationUser CreateTestApplicationUser(int id = 1, string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsDeleted = false
        };
    }

    private static User CreateDeletedUser(int id = 999, int aspNetUserId = 999)
    {
        var user = CreateTestUser(id, aspNetUserId);
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        return user;
    }

    #endregion

    #region User Creation Tests

    [Fact]
    public void CreateUser_ShouldHaveValidProperties()
    {
        // Act
        var user = CreateTestUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal(1, user.AspNetUserId);
        Assert.False(user.IsDeleted);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
        Assert.True(user.UpdatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(42, 99)]
    [InlineData(1, 1)]
    [InlineData(100, 200)]
    public void CreateUser_ShouldAcceptCustomParameters(int userId, int aspNetUserId)
    {
        // Act
        var user = CreateTestUser(userId, aspNetUserId);

        // Assert
        Assert.Equal(userId, user.Id);
        Assert.Equal(aspNetUserId, user.AspNetUserId);
    }

    #endregion

    #region ApplicationUser Tests

    [Fact]
    public void CreateApplicationUser_ShouldHaveValidProperties()
    {
        // Act
        var appUser = CreateTestApplicationUser();

        // Assert
        Assert.NotNull(appUser);
        Assert.Equal(1, appUser.Id);
        Assert.Equal("test@example.com", appUser.Email);
        Assert.Equal("TEST@EXAMPLE.COM", appUser.NormalizedEmail);
        Assert.Equal("Test", appUser.FirstName);
        Assert.Equal("User", appUser.LastName);
        Assert.True(appUser.EmailConfirmed);
        Assert.False(appUser.IsDeleted);
    }

    [Theory]
    [InlineData("user@domain.com")]
    [InlineData("admin@company.org")]
    [InlineData("test.user+tag@example.co.uk")]
    public void CreateApplicationUser_ShouldHandleValidEmails(string email)
    {
        // Act
        var appUser = CreateTestApplicationUser(email: email);

        // Assert
        Assert.Equal(email, appUser.Email);
        Assert.Equal(email.ToUpper(), appUser.NormalizedEmail);
        Assert.Equal(email, appUser.UserName);
        Assert.Equal(email.ToUpper(), appUser.NormalizedUserName);
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public void CreateDeletedUser_ShouldBeMarkedAsDeleted()
    {
        // Act
        var deletedUser = CreateDeletedUser();

        // Assert
        Assert.True(deletedUser.IsDeleted);
        Assert.Equal(999, deletedUser.Id);
        Assert.Equal(999, deletedUser.AspNetUserId);
        Assert.True(deletedUser.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void User_ShouldInheritFromBaseEntity()
    {
        // Act
        var user = CreateTestUser();

        // Assert - Verify BaseEntity properties exist
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
        Assert.True(user.UpdatedAt <= DateTime.UtcNow);
        Assert.False(user.IsDeleted);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void User_ShouldMaintainAuditTrail()
    {
        // Arrange
        var createdTime = DateTime.UtcNow.AddDays(-5);
        var updatedTime = DateTime.UtcNow;

        // Act
        var user = new User
        {
            Id = 1,
            AspNetUserId = 1,
            CreatedAt = createdTime,
            UpdatedAt = updatedTime,
            IsDeleted = false
        };

        // Assert
        Assert.Equal(createdTime, user.CreatedAt);
        Assert.Equal(updatedTime, user.UpdatedAt);
        Assert.True(user.UpdatedAt >= user.CreatedAt);
    }

    #endregion
}
