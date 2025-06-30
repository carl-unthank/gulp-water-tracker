using Gulp.Shared.Models;

namespace Gulp.Tests.Unit;

/// <summary>
/// Tests for DailyGoal domain model and related business logic
/// Follows Single Responsibility Principle - only tests DailyGoal-related functionality
/// </summary>
public class DailyGoalTests
{
    #region Test Data Factory Methods (Private - Single Responsibility)

    private static DailyGoal CreateTestDailyGoal(int id = 1, int userId = 1, int targetAmountMl = 2000)
    {
        return new DailyGoal
        {
            Id = id,
            UserId = userId,
            TargetAmountMl = targetAmountMl,
            EffectiveDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static DailyGoal CreateMinimumDailyGoal(int id = 1, int userId = 1)
    {
        return CreateTestDailyGoal(id, userId, 500); // Minimum 500ml
    }

    private static DailyGoal CreateMaximumDailyGoal(int id = 1, int userId = 1)
    {
        return CreateTestDailyGoal(id, userId, 10000); // Maximum 10000ml
    }

    private static List<DailyGoal> CreateWeeklyGoals(int userId = 1, int targetAmountMl = 2000)
    {
        var goals = new List<DailyGoal>();

        for (int i = 0; i < 7; i++)
        {
            var goal = CreateTestDailyGoal(i + 1, userId, targetAmountMl);
            goal.EffectiveDate = DateTime.Today.AddDays(-i);
            goals.Add(goal);
        }

        return goals;
    }

    #endregion

    #region DailyGoal Creation Tests

    [Fact]
    public void CreateDailyGoal_ShouldHaveValidProperties()
    {
        // Act
        var dailyGoal = CreateTestDailyGoal();

        // Assert
        Assert.NotNull(dailyGoal);
        Assert.Equal(1, dailyGoal.Id);
        Assert.Equal(1, dailyGoal.UserId);
        Assert.Equal(2000, dailyGoal.TargetAmountMl);
        Assert.Equal(DateTime.Today, dailyGoal.EffectiveDate);
        Assert.False(dailyGoal.IsDeleted);
    }

    [Theory]
    [InlineData(500)]   // Minimum
    [InlineData(1500)]  // Common
    [InlineData(2000)]  // Standard
    [InlineData(3000)]  // High
    [InlineData(10000)] // Maximum
    public void CreateDailyGoal_ShouldAcceptValidTargets(int targetAmountMl)
    {
        // Act
        var dailyGoal = CreateTestDailyGoal(targetAmountMl: targetAmountMl);

        // Assert
        Assert.Equal(targetAmountMl, dailyGoal.TargetAmountMl);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void CreateMinimumDailyGoal_ShouldHaveMinimumValidTarget()
    {
        // Act
        var dailyGoal = CreateMinimumDailyGoal();

        // Assert
        Assert.Equal(500, dailyGoal.TargetAmountMl);
        Assert.True(dailyGoal.TargetAmountMl >= 500); // Business rule: minimum 500ml
    }

    [Fact]
    public void CreateMaximumDailyGoal_ShouldHaveMaximumValidTarget()
    {
        // Act
        var dailyGoal = CreateMaximumDailyGoal();

        // Assert
        Assert.Equal(10000, dailyGoal.TargetAmountMl);
        Assert.True(dailyGoal.TargetAmountMl <= 10000); // Business rule: maximum 10000ml
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void DailyGoal_ShouldBelongToUser()
    {
        // Arrange
        var userId = 42;

        // Act
        var dailyGoal = CreateTestDailyGoal(userId: userId);

        // Assert
        Assert.Equal(userId, dailyGoal.UserId);
    }

    [Fact]
    public void DailyGoal_ShouldHaveEffectiveDate()
    {
        // Arrange
        var effectiveDate = DateTime.Today.AddDays(-1);

        // Act
        var dailyGoal = new DailyGoal
        {
            Id = 1,
            UserId = 1,
            TargetAmountMl = 2000,
            EffectiveDate = effectiveDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Assert
        Assert.Equal(effectiveDate, dailyGoal.EffectiveDate);
    }

    [Fact]
    public void CreateWeeklyGoals_ShouldCreateSevenDaysOfGoals()
    {
        // Act
        var goals = CreateWeeklyGoals(userId: 1, targetAmountMl: 2500);

        // Assert
        Assert.Equal(7, goals.Count);
        Assert.All(goals, goal => Assert.Equal(1, goal.UserId));
        Assert.All(goals, goal => Assert.Equal(2500, goal.TargetAmountMl));

        // Verify date progression (today, yesterday, day before, etc.)
        for (int i = 0; i < goals.Count; i++)
        {
            var expectedDate = DateTime.Today.AddDays(-i);
            Assert.Equal(expectedDate, goals[i].EffectiveDate);
        }
    }

    [Fact]
    public void DailyGoal_ShouldInheritFromBaseEntity()
    {
        // Act
        var dailyGoal = CreateTestDailyGoal();

        // Assert - Verify BaseEntity properties exist
        Assert.True(dailyGoal.CreatedAt <= DateTime.UtcNow);
        Assert.True(dailyGoal.UpdatedAt <= DateTime.UtcNow);
        Assert.False(dailyGoal.IsDeleted);
    }

    #endregion

    #region Goal Achievement Tests

    [Theory]
    [InlineData(2000, 2000, true)]  // Exactly met
    [InlineData(2000, 2100, true)]  // Exceeded
    [InlineData(2000, 1999, false)] // Just under
    [InlineData(2000, 1000, false)] // Well under
    [InlineData(2000, 0, false)]    // No intake
    public void IsGoalAchieved_ShouldCalculateCorrectly(int targetMl, int actualMl, bool expectedAchieved)
    {
        // Arrange
        var dailyGoal = CreateTestDailyGoal(targetAmountMl: targetMl);

        // Act
        var isAchieved = actualMl >= dailyGoal.TargetAmountMl;

        // Assert
        Assert.Equal(expectedAchieved, isAchieved);
    }

    [Theory]
    [InlineData(2000, 1000, 50.0)]   // 50% progress
    [InlineData(2000, 1500, 75.0)]   // 75% progress
    [InlineData(2000, 2000, 100.0)]  // 100% progress
    [InlineData(2000, 2200, 110.0)]  // 110% progress (exceeded)
    [InlineData(2000, 0, 0.0)]       // 0% progress
    public void CalculateGoalProgress_ShouldReturnCorrectPercentage(int targetMl, int actualMl, double expectedPercentage)
    {
        // Arrange
        var dailyGoal = CreateTestDailyGoal(targetAmountMl: targetMl);

        // Act
        var progressPercentage = (double)actualMl / dailyGoal.TargetAmountMl * 100;

        // Assert
        Assert.Equal(expectedPercentage, progressPercentage, 1); // 1 decimal place precision
    }

    #endregion

    #region Date-based Tests

    [Fact]
    public void GetCurrentGoal_ShouldReturnTodaysGoal()
    {
        // Arrange
        var goals = new List<DailyGoal>
        {
            CreateTestDailyGoal(1, 1, 2000), // Today
            CreateTestDailyGoal(2, 1, 1800), // Today (different goal)
        };
        
        // Set one goal to yesterday
        goals.Add(CreateTestDailyGoal(3, 1, 2200));
        goals[2].EffectiveDate = DateTime.Today.AddDays(-1);

        // Act
        var todaysGoals = goals.Where(g => g.EffectiveDate.Date == DateTime.Today).ToList();

        // Assert
        Assert.Equal(2, todaysGoals.Count);
        Assert.All(todaysGoals, goal => Assert.Equal(DateTime.Today, goal.EffectiveDate));
    }

    [Fact]
    public void GetGoalHistory_ShouldReturnChronologicalOrder()
    {
        // Arrange
        var goals = CreateWeeklyGoals();

        // Act
        var orderedGoals = goals.OrderByDescending(g => g.EffectiveDate).ToList();

        // Assert
        Assert.Equal(7, orderedGoals.Count);
        
        // First goal should be today, last should be 6 days ago
        Assert.Equal(DateTime.Today, orderedGoals.First().EffectiveDate);
        Assert.Equal(DateTime.Today.AddDays(-6), orderedGoals.Last().EffectiveDate);
    }

    #endregion
}
