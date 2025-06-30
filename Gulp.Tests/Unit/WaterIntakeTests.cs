using Gulp.Shared.Models;

namespace Gulp.Tests.Unit;

/// <summary>
/// Tests for WaterIntake domain model and related business logic
/// Follows Single Responsibility Principle - only tests WaterIntake-related functionality
/// </summary>
public class WaterIntakeTests
{
    #region Test Data Factory Methods (Private - Single Responsibility)

    private static WaterIntake CreateTestWaterIntake(int id = 1, int userId = 1, int amountMl = 250)
    {
        return new WaterIntake
        {
            Id = id,
            UserId = userId,
            AmountMl = amountMl,
            ConsumedAt = DateTime.UtcNow,
            Notes = "Test water intake",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    private static WaterIntake CreateMinimumWaterIntake(int id = 1, int userId = 1)
    {
        return CreateTestWaterIntake(id, userId, 1); // Minimum 1ml
    }

    private static WaterIntake CreateMaximumWaterIntake(int id = 1, int userId = 1)
    {
        return CreateTestWaterIntake(id, userId, 10000); // Maximum 10000ml
    }

    private static List<WaterIntake> CreateDailyWaterIntakes(int userId = 1, int count = 4)
    {
        var intakes = new List<WaterIntake>();
        var baseTime = DateTime.Today.AddHours(8); // Start at 8 AM

        for (int i = 0; i < count; i++)
        {
            var intake = CreateTestWaterIntake(i + 1, userId, 250);
            intake.ConsumedAt = baseTime.AddHours(i * 2); // Every 2 hours
            intakes.Add(intake);
        }

        return intakes;
    }

    #endregion

    #region WaterIntake Creation Tests

    [Fact]
    public void CreateWaterIntake_ShouldHaveValidProperties()
    {
        // Act
        var waterIntake = CreateTestWaterIntake();

        // Assert
        Assert.NotNull(waterIntake);
        Assert.Equal(1, waterIntake.Id);
        Assert.Equal(1, waterIntake.UserId);
        Assert.Equal(250, waterIntake.AmountMl);
        Assert.False(waterIntake.IsDeleted);
        Assert.True(waterIntake.ConsumedAt <= DateTime.UtcNow);
        Assert.NotNull(waterIntake.Notes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void CreateWaterIntake_ShouldAcceptValidAmounts(int amountMl)
    {
        // Act
        var waterIntake = CreateTestWaterIntake(amountMl: amountMl);

        // Assert
        Assert.Equal(amountMl, waterIntake.AmountMl);
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public void CreateMinimumWaterIntake_ShouldHaveMinimumValidAmount()
    {
        // Act
        var waterIntake = CreateMinimumWaterIntake();

        // Assert
        Assert.Equal(1, waterIntake.AmountMl);
        Assert.True(waterIntake.AmountMl >= 1); // Business rule: minimum 1ml
    }

    [Fact]
    public void CreateMaximumWaterIntake_ShouldHaveMaximumValidAmount()
    {
        // Act
        var waterIntake = CreateMaximumWaterIntake();

        // Assert
        Assert.Equal(10000, waterIntake.AmountMl);
        Assert.True(waterIntake.AmountMl <= 10000); // Business rule: maximum 10000ml
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void WaterIntake_ShouldBelongToUser()
    {
        // Arrange
        var userId = 42;

        // Act
        var waterIntake = CreateTestWaterIntake(userId: userId);

        // Assert
        Assert.Equal(userId, waterIntake.UserId);
    }

    [Fact]
    public void WaterIntake_ShouldHaveConsumedTime()
    {
        // Arrange
        var consumedTime = DateTime.UtcNow.AddHours(-2);

        // Act
        var waterIntake = new WaterIntake
        {
            Id = 1,
            UserId = 1,
            AmountMl = 250,
            ConsumedAt = consumedTime,
            Notes = "Test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Assert
        Assert.Equal(consumedTime, waterIntake.ConsumedAt);
    }

    [Fact]
    public void CreateDailyWaterIntakes_ShouldCreateRealisticSchedule()
    {
        // Act
        var intakes = CreateDailyWaterIntakes(userId: 1, count: 4);

        // Assert
        Assert.Equal(4, intakes.Count);
        Assert.All(intakes, intake => Assert.Equal(1, intake.UserId));
        Assert.All(intakes, intake => Assert.Equal(250, intake.AmountMl));

        // Verify time progression (every 2 hours starting at 8 AM)
        var expectedTimes = new[]
        {
            DateTime.Today.AddHours(8),  // 8 AM
            DateTime.Today.AddHours(10), // 10 AM
            DateTime.Today.AddHours(12), // 12 PM
            DateTime.Today.AddHours(14)  // 2 PM
        };

        for (int i = 0; i < intakes.Count; i++)
        {
            Assert.Equal(expectedTimes[i], intakes[i].ConsumedAt);
        }
    }

    [Fact]
    public void WaterIntake_ShouldInheritFromBaseEntity()
    {
        // Act
        var waterIntake = CreateTestWaterIntake();

        // Assert - Verify BaseEntity properties exist
        Assert.True(waterIntake.CreatedAt <= DateTime.UtcNow);
        Assert.True(waterIntake.UpdatedAt <= DateTime.UtcNow);
        Assert.False(waterIntake.IsDeleted);
    }

    #endregion

    #region Aggregation Tests

    [Fact]
    public void CalculateDailyTotal_ShouldSumAllIntakes()
    {
        // Arrange
        var intakes = CreateDailyWaterIntakes(count: 4); // 4 x 250ml = 1000ml

        // Act
        var totalAmount = intakes.Sum(i => i.AmountMl);

        // Assert
        Assert.Equal(1000, totalAmount);
    }

    [Fact]
    public void FilterIntakesByDate_ShouldReturnTodaysIntakes()
    {
        // Arrange
        var intakes = new List<WaterIntake>
        {
            CreateTestWaterIntake(1, 1, 250), // Today
            CreateTestWaterIntake(2, 1, 300), // Today
        };
        
        // Set one intake to yesterday
        intakes.Add(CreateTestWaterIntake(3, 1, 200));
        intakes[2].ConsumedAt = DateTime.Today.AddDays(-1);

        // Act
        var todaysIntakes = intakes.Where(i => i.ConsumedAt.Date == DateTime.Today).ToList();

        // Assert
        Assert.Equal(2, todaysIntakes.Count);
        Assert.Equal(550, todaysIntakes.Sum(i => i.AmountMl)); // 250 + 300
    }

    #endregion
}
