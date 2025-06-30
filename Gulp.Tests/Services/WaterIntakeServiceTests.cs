using Microsoft.Extensions.Logging;
using Moq;
using Gulp.Infrastructure.Services;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;
using Gulp.Shared.DTOs;
using System.Linq.Expressions;

namespace Gulp.Tests.Services;

public class WaterIntakeServiceTests
{
    private readonly Mock<IRepository<WaterIntake>> _mockWaterIntakeRepository;
    private readonly Mock<IRepository<DailyGoal>> _mockDailyGoalRepository;
    private readonly Mock<IDailyGoalService> _mockDailyGoalService;
    private readonly Mock<ILogger<WaterIntakeService>> _mockLogger;
    private readonly WaterIntakeService _service;

    public WaterIntakeServiceTests()
    {
        _mockWaterIntakeRepository = new Mock<IRepository<WaterIntake>>();
        _mockDailyGoalRepository = new Mock<IRepository<DailyGoal>>();
        _mockDailyGoalService = new Mock<IDailyGoalService>();
        _mockLogger = new Mock<ILogger<WaterIntakeService>>();

        _service = new WaterIntakeService(
            _mockWaterIntakeRepository.Object,
            _mockDailyGoalRepository.Object,
            _mockDailyGoalService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RecordWaterIntakeAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var createDto = new CreateWaterIntakeDto
        {
            AmountMl = 250,
            ConsumedAt = DateTime.UtcNow,
            Notes = "Morning water"
        };

        _mockWaterIntakeRepository
            .Setup(r => r.AddAsync(It.IsAny<WaterIntake>()))
            .Callback<WaterIntake>(w => w.Id = 1)
            .ReturnsAsync((WaterIntake w) => w);

        // Act
        var result = await _service.RecordWaterIntakeAsync(userId, createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal(createDto.AmountMl, result.Value.AmountMl);
        Assert.Equal(createDto.ConsumedAt, result.Value.ConsumedAt);
        Assert.Equal(createDto.Notes, result.Value.Notes);

        _mockWaterIntakeRepository.Verify(r => r.AddAsync(It.Is<WaterIntake>(w =>
            w.UserId == userId &&
            w.AmountMl == createDto.AmountMl &&
            w.ConsumedAt == createDto.ConsumedAt &&
            w.Notes == createDto.Notes)), Times.Once);
    }

    [Fact]
    public async Task GetWaterIntakeByIdAsync_ExistingRecord_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var waterIntakeId = 1;
        var waterIntake = new WaterIntake
        {
            Id = waterIntakeId,
            UserId = userId,
            AmountMl = 250,
            ConsumedAt = DateTime.UtcNow,
            Notes = "Test water"
        };

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync(waterIntake);

        // Act
        var result = await _service.GetWaterIntakeByIdAsync(waterIntakeId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(waterIntake.Id, result.Value.Id);
        Assert.Equal(waterIntake.AmountMl, result.Value.AmountMl);
        Assert.Equal(waterIntake.ConsumedAt, result.Value.ConsumedAt);
        Assert.Equal(waterIntake.Notes, result.Value.Notes);
    }

    [Fact]
    public async Task GetWaterIntakeByIdAsync_NonExistentRecord_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var waterIntakeId = 999;

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync((WaterIntake?)null);

        // Act
        var result = await _service.GetWaterIntakeByIdAsync(waterIntakeId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("Water intake record not found", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWaterIntakeByIdAsync_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var waterIntakeId = 1;
        var waterIntake = new WaterIntake
        {
            Id = waterIntakeId,
            UserId = otherUserId, // Different user
            AmountMl = 250,
            ConsumedAt = DateTime.UtcNow
        };

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync(waterIntake);

        // Act
        var result = await _service.GetWaterIntakeByIdAsync(waterIntakeId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
        Assert.Equal("Access denied", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWaterIntakeByDateAsync_ValidDate_ReturnsWaterIntakes()
    {
        // Arrange
        var userId = 1;
        var date = DateOnly.FromDateTime(DateTime.Today);
        var waterIntakes = new List<WaterIntake>
        {
            new() { Id = 1, UserId = userId, AmountMl = 250, ConsumedAt = DateTime.Today.AddHours(8), Notes = "Morning" },
            new() { Id = 2, UserId = userId, AmountMl = 300, ConsumedAt = DateTime.Today.AddHours(12), Notes = "Lunch" }
        };

        _mockWaterIntakeRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<WaterIntake, bool>>>()))
            .ReturnsAsync(waterIntakes);

        // Act
        var result = await _service.GetWaterIntakeByDateAsync(userId, date);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count());

        // Verify the service correctly maps domain models to DTOs
        var dtoList = result.Value.ToList();
        Assert.Equal(1, dtoList[0].Id);
        Assert.Equal(250, dtoList[0].AmountMl);
        Assert.Equal("Morning", dtoList[0].Notes);
        Assert.Equal(2, dtoList[1].Id);
        Assert.Equal(300, dtoList[1].AmountMl);
        Assert.Equal("Lunch", dtoList[1].Notes);

        // Verify repository was called with correct filter
        _mockWaterIntakeRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<WaterIntake, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWaterIntakeAsync_ValidUpdate_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var waterIntakeId = 1;
        var existingWaterIntake = new WaterIntake
        {
            Id = waterIntakeId,
            UserId = userId,
            AmountMl = 250,
            ConsumedAt = DateTime.UtcNow.AddHours(-1),
            Notes = "Old notes"
        };

        var updateDto = new UpdateWaterIntakeDto
        {
            AmountMl = 300,
            ConsumedAt = DateTime.UtcNow,
            Notes = "Updated notes"
        };

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync(existingWaterIntake);

        _mockWaterIntakeRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WaterIntake>()))
            .ReturnsAsync((WaterIntake w) => w);

        // Act
        var result = await _service.UpdateWaterIntakeAsync(waterIntakeId, userId, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(updateDto.AmountMl, result.Value.AmountMl);
        Assert.Equal(updateDto.ConsumedAt, result.Value.ConsumedAt);
        Assert.Equal(updateDto.Notes, result.Value.Notes);

        _mockWaterIntakeRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterIntake>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWaterIntakeAsync_ValidDelete_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var waterIntakeId = 1;
        var waterIntake = new WaterIntake
        {
            Id = waterIntakeId,
            UserId = userId,
            AmountMl = 250,
            ConsumedAt = DateTime.UtcNow
        };

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync(waterIntake);

        _mockWaterIntakeRepository
            .Setup(r => r.DeleteAsync(waterIntake))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteWaterIntakeAsync(waterIntakeId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        _mockWaterIntakeRepository.Verify(r => r.DeleteAsync(waterIntake), Times.Once);
    }

    [Fact]
    public async Task DeleteWaterIntakeAsync_NonExistentRecord_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var waterIntakeId = 999;

        _mockWaterIntakeRepository
            .Setup(r => r.GetByIdAsync(waterIntakeId))
            .ReturnsAsync((WaterIntake?)null);

        // Act
        var result = await _service.DeleteWaterIntakeAsync(waterIntakeId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal("Water intake record not found", result.ErrorMessage);
    }
}
