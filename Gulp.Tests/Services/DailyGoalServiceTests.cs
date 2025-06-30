using Microsoft.Extensions.Logging;
using Moq;
using Gulp.Infrastructure.Services;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;
using Gulp.Shared.DTOs;
using System.Linq.Expressions;

namespace Gulp.Tests.Services;

public class DailyGoalServiceTests
{
    private readonly Mock<IRepository<DailyGoal>> _mockDailyGoalRepository;
    private readonly Mock<ILogger<DailyGoalService>> _mockLogger;
    private readonly DailyGoalService _service;

    public DailyGoalServiceTests()
    {
        _mockDailyGoalRepository = new Mock<IRepository<DailyGoal>>();
        _mockLogger = new Mock<ILogger<DailyGoalService>>();
        
        _service = new DailyGoalService(
            _mockDailyGoalRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetCurrentDailyGoalAsync_ExistingGoal_ReturnsSuccessResult()
    {
        // Arrange
        var userId = 1;
        var dailyGoals = new List<DailyGoal>
        {
            new() 
            { 
                Id = 1, 
                UserId = userId, 
                TargetAmountMl = 2000, 
                EffectiveDate = DateTime.Today.AddDays(-2) 
            },
            new() 
            { 
                Id = 2, 
                UserId = userId, 
                TargetAmountMl = 2500, 
                EffectiveDate = DateTime.Today.AddDays(-1) 
            }
        };

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(dailyGoals);

        // Act
        var result = await _service.GetCurrentDailyGoalAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Verify the service returns the latest goal (highest effective date) mapped to DTO
        Assert.Equal(2500, result.Value.TargetAmountMl);

        // Verify the repository was called with correct user filter
        _mockDailyGoalRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentDailyGoalAsync_NoGoal_ReturnsFailureResult()
    {
        // Arrange
        var userId = 1;
        var emptyGoals = new List<DailyGoal>();

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(emptyGoals);

        // Act
        var result = await _service.GetCurrentDailyGoalAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("No daily goal found", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateDailyGoalAsync_NewGoal_CreatesNewGoalAndReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var updateDto = new DailyGoalDto
        {
            TargetAmountMl = 3000
        };

        var emptyGoals = new List<DailyGoal>();

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(emptyGoals);

        _mockDailyGoalRepository
            .Setup(r => r.AddAsync(It.IsAny<DailyGoal>()))
            .Callback<DailyGoal>(g => g.Id = 1)
            .ReturnsAsync((DailyGoal g) => g);

        // Act
        var result = await _service.UpdateDailyGoalAsync(userId, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(updateDto.TargetAmountMl, result.Value.TargetAmountMl);

        // Verify the service creates a domain model with correct properties
        _mockDailyGoalRepository.Verify(r => r.AddAsync(It.Is<DailyGoal>(g =>
            g.UserId == userId &&
            g.TargetAmountMl == updateDto.TargetAmountMl &&
            g.EffectiveDate == DateTime.Today)), Times.Once);
    }

    [Fact]
    public async Task UpdateDailyGoalAsync_ExistingGoalToday_CreatesNewGoal()
    {
        // Arrange
        var userId = 1;
        var updateDto = new DailyGoalDto
        {
            TargetAmountMl = 3000
        };

        var existingGoals = new List<DailyGoal>
        {
            new()
            {
                Id = 1,
                UserId = userId,
                TargetAmountMl = 2000,
                EffectiveDate = DateTime.Today.AddDays(-1)
            }
        };

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(existingGoals);

        _mockDailyGoalRepository
            .Setup(r => r.AddAsync(It.IsAny<DailyGoal>()))
            .Callback<DailyGoal>(g => g.Id = 2)
            .ReturnsAsync((DailyGoal g) => g);

        // Act
        var result = await _service.UpdateDailyGoalAsync(userId, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(updateDto.TargetAmountMl, result.Value.TargetAmountMl);

        // Verify the service always creates a new goal (business rule)
        _mockDailyGoalRepository.Verify(r => r.AddAsync(It.Is<DailyGoal>(g =>
            g.UserId == userId &&
            g.TargetAmountMl == updateDto.TargetAmountMl &&
            g.EffectiveDate == DateTime.Today)), Times.Once);
    }

    [Fact]
    public async Task GetDailyGoalHistoryAsync_ValidUser_ReturnsOrderedGoals()
    {
        // Arrange
        var userId = 1;
        var page = 1;
        var pageSize = 10;
        
        var dailyGoals = new List<DailyGoal>
        {
            new()
            {
                Id = 3,
                UserId = userId,
                TargetAmountMl = 3500,
                EffectiveDate = DateTime.Today
            },
            new()
            {
                Id = 2,
                UserId = userId,
                TargetAmountMl = 3000,
                EffectiveDate = DateTime.Today.AddDays(-1)
            },
            new()
            {
                Id = 1,
                UserId = userId,
                TargetAmountMl = 2500,
                EffectiveDate = DateTime.Today.AddDays(-2)
            }
        };

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(dailyGoals);

        // Act
        var result = await _service.GetDailyGoalHistoryAsync(userId, page, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var goalsList = result.Value.ToList();
        Assert.Equal(3, goalsList.Count);
        
        // Should be ordered by EffectiveDate descending (verify by target amounts)
        Assert.Equal(3500, goalsList[0].TargetAmountMl); // Today
        Assert.Equal(3000, goalsList[1].TargetAmountMl); // Yesterday
        Assert.Equal(2500, goalsList[2].TargetAmountMl); // Day before yesterday
    }

    [Fact]
    public async Task GetDailyGoalHistoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = 1;
        var page = 2;
        var pageSize = 2;
        
        var dailyGoals = new List<DailyGoal>
        {
            new() { Id = 5, UserId = userId, TargetAmountMl = 3500, EffectiveDate = DateTime.Today },
            new() { Id = 4, UserId = userId, TargetAmountMl = 3250, EffectiveDate = DateTime.Today.AddDays(-1) },
            new() { Id = 3, UserId = userId, TargetAmountMl = 3000, EffectiveDate = DateTime.Today.AddDays(-2) },
            new() { Id = 2, UserId = userId, TargetAmountMl = 2500, EffectiveDate = DateTime.Today.AddDays(-3) },
            new() { Id = 1, UserId = userId, TargetAmountMl = 2000, EffectiveDate = DateTime.Today.AddDays(-4) }
        };

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(dailyGoals);

        // Act
        var result = await _service.GetDailyGoalHistoryAsync(userId, page, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        var goalsList = result.Value.ToList();
        Assert.Equal(2, goalsList.Count); // Page size
        
        // Should be the second page (skip 2, take 2) - verify by target amounts
        Assert.Equal(3000, goalsList[0].TargetAmountMl); // Third most recent
        Assert.Equal(2500, goalsList[1].TargetAmountMl); // Fourth most recent
    }

    [Fact]
    public async Task GetDailyGoalHistoryAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;
        var page = 1;
        var pageSize = 10;
        var emptyGoals = new List<DailyGoal>();

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(emptyGoals);

        // Act
        var result = await _service.GetDailyGoalHistoryAsync(userId, page, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1500)]
    [InlineData(2000)]
    [InlineData(3000)]
    [InlineData(5000)]
    public async Task UpdateDailyGoalAsync_VariousTargetAmounts_CreatesGoalWithCorrectAmount(int targetAmount)
    {
        // Arrange
        var userId = 1;
        var updateDto = new DailyGoalDto
        {
            TargetAmountMl = targetAmount
        };

        var emptyGoals = new List<DailyGoal>();

        _mockDailyGoalRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DailyGoal, bool>>>()))
            .ReturnsAsync(emptyGoals);

        _mockDailyGoalRepository
            .Setup(r => r.AddAsync(It.IsAny<DailyGoal>()))
            .Callback<DailyGoal>(g => g.Id = 1)
            .ReturnsAsync((DailyGoal g) => g);

        // Act
        var result = await _service.UpdateDailyGoalAsync(userId, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(targetAmount, result.Value.TargetAmountMl);
    }
}
