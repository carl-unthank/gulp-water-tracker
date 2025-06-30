using Microsoft.Extensions.Logging;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Common;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Services;

public class WaterIntakeService : IWaterIntakeService
{
    private readonly IRepository<WaterIntake> _waterIntakeRepository;
    private readonly IRepository<DailyGoal> _dailyGoalRepository;
    private readonly IDailyGoalService _dailyGoalService;
    private readonly ILogger<WaterIntakeService> _logger;

    public WaterIntakeService(
        IRepository<WaterIntake> waterIntakeRepository,
        IRepository<DailyGoal> dailyGoalRepository,
        IDailyGoalService dailyGoalService,
        ILogger<WaterIntakeService> logger)
    {
        _waterIntakeRepository = waterIntakeRepository;
        _dailyGoalRepository = dailyGoalRepository;
        _dailyGoalService = dailyGoalService;
        _logger = logger;
    }

    public async Task<Result<WaterIntakeDto>> RecordWaterIntakeAsync(int userId, CreateWaterIntakeDto createDto)
    {
        try
        {
            var waterIntake = new WaterIntake
            {
                UserId = userId,
                AmountMl = createDto.AmountMl,
                ConsumedAt = createDto.ConsumedAt,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _waterIntakeRepository.AddAsync(waterIntake);

            var dto = MapToDto(waterIntake);
            return Result<WaterIntakeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording water intake for user {UserId}", userId);
            return Result<WaterIntakeDto>.Failure(ex, "An error occurred while recording water intake");
        }
    }

    public async Task<Result<WaterIntakeDto>> GetWaterIntakeByIdAsync(int id, int userId)
    {
        try
        {
            var waterIntake = await _waterIntakeRepository.GetByIdAsync(id);
            
            if (waterIntake == null)
            {
                return Result<WaterIntakeDto>.Failure("Water intake record not found", "NOT_FOUND");
            }

            if (waterIntake.UserId != userId)
            {
                return Result<WaterIntakeDto>.Failure("Access denied", "UNAUTHORIZED");
            }

            var dto = MapToDto(waterIntake);
            return Result<WaterIntakeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting water intake {Id} for user {UserId}", id, userId);
            return Result<WaterIntakeDto>.Failure(ex, "An error occurred while retrieving water intake");
        }
    }

    public async Task<Result<IEnumerable<WaterIntakeDto>>> GetTodaysWaterIntakeAsync(int userId)
    {
        return await GetWaterIntakeByDateAsync(userId, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<Result<IEnumerable<WaterIntakeDto>>> GetWaterIntakeByDateAsync(int userId, DateOnly date)
    {
        try
        {
                var startDate = date.ToDateTime(TimeOnly.MinValue);
            var endDate = date.ToDateTime(TimeOnly.MaxValue);

            var waterIntakes = await _waterIntakeRepository.FindAsync(
                w => w.UserId == userId &&
                     w.ConsumedAt >= startDate &&
                     w.ConsumedAt <= endDate);

            var dtos = waterIntakes.Select(MapToDto).ToList();
            return Result<IEnumerable<WaterIntakeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting water intake for date {Date} for user {UserId}", date, userId);
            return Result<IEnumerable<WaterIntakeDto>>.Failure(ex, "An error occurred while retrieving water intake");
        }
    }

    public async Task<Result<WaterIntakeDto>> UpdateWaterIntakeAsync(int id, int userId, UpdateWaterIntakeDto updateDto)
    {
        try
        {
            var waterIntake = await _waterIntakeRepository.GetByIdAsync(id);
            
            if (waterIntake == null)
            {
                return Result<WaterIntakeDto>.Failure("Water intake record not found", "NOT_FOUND");
            }

            if (waterIntake.UserId != userId)
            {
                return Result<WaterIntakeDto>.Failure("Access denied", "UNAUTHORIZED");
            }

            waterIntake.AmountMl = updateDto.AmountMl;
            waterIntake.ConsumedAt = updateDto.ConsumedAt;
            waterIntake.Notes = updateDto.Notes;
            waterIntake.UpdatedAt = DateTime.UtcNow;

            await _waterIntakeRepository.UpdateAsync(waterIntake);

            var dto = MapToDto(waterIntake);
            return Result<WaterIntakeDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating water intake {Id} for user {UserId}", id, userId);
            return Result<WaterIntakeDto>.Failure(ex, "An error occurred while updating water intake");
        }
    }

    public async Task<Result> DeleteWaterIntakeAsync(int id, int userId)
    {
        try
        {
            var waterIntake = await _waterIntakeRepository.GetByIdAsync(id);
            
            if (waterIntake == null)
            {
                return Result.Failure("Water intake record not found", "NOT_FOUND");
            }

            if (waterIntake.UserId != userId)
            {
                return Result.Failure("Access denied", "UNAUTHORIZED");
            }

            await _waterIntakeRepository.DeleteAsync(waterIntake);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting water intake {Id} for user {UserId}", id, userId);
            return Result.Failure(ex, "An error occurred while deleting water intake");
        }
    }

    public async Task<Result<HistoryDto>> GetDailyProgressAsync(int userId)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todaysIntakes = await GetWaterIntakeByDateAsync(userId, today);
            
            if (!todaysIntakes.IsSuccess)
            {
                return Result<HistoryDto>.Failure(todaysIntakes.ErrorMessage, todaysIntakes.ErrorCode);
            }

            // Get the goal that was active for this specific date
            var goalResult = await _dailyGoalService.GetDailyGoalForDateAsync(userId, today.ToDateTime(TimeOnly.MinValue));
            var goalMl = goalResult.IsSuccess ? goalResult.Value.TargetAmountMl : 2000;
            
            var intakes = todaysIntakes.Value.ToList();
            var totalMl = intakes.Sum(i => i.AmountMl);
            
            var progress = new HistoryDto
            {
                Date = today.ToDateTime(TimeOnly.MinValue),
                TotalMl = totalMl,
                GoalMl = goalMl,
                ProgressPercentage = goalMl > 0 ? (double)totalMl / goalMl * 100 : 0,
                GoalAchieved = totalMl >= goalMl,
                Intakes = intakes
            };

            return Result<HistoryDto>.Success(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily progress for user {UserId}", userId);
            return Result<HistoryDto>.Failure(ex, "An error occurred while retrieving daily progress");
        }
    }

    public async Task<Result<IEnumerable<HistoryDto>>> GetWaterIntakeHistoryAsync(int userId, DateOnly startDate, DateOnly endDate, int page, int pageSize)
    {
        try
        {
            // Get all water intakes for the date range
            var intakes = await _waterIntakeRepository.FindAsync(w =>
                w.UserId == userId &&
                w.ConsumedAt.Date >= startDate.ToDateTime(TimeOnly.MinValue) &&
                w.ConsumedAt.Date <= endDate.ToDateTime(TimeOnly.MinValue));

            // Group intakes by date
            var intakesByDate = intakes
                .GroupBy(w => DateOnly.FromDateTime(w.ConsumedAt.Date))
                .OrderByDescending(g => g.Key)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var historyList = new List<HistoryDto>();

            foreach (var dateGroup in intakesByDate)
            {
                var date = dateGroup.Key;
                var dayIntakes = dateGroup.OrderBy(w => w.ConsumedAt).ToList();
                var totalMl = dayIntakes.Sum(w => w.AmountMl);

                // Get the goal that was active for this specific date
                var goalResult = await _dailyGoalService.GetDailyGoalForDateAsync(userId, date.ToDateTime(TimeOnly.MinValue));
                var goalMl = goalResult.IsSuccess ? goalResult.Value.TargetAmountMl : 2000;

                var progressPercentage = goalMl > 0 ? (double)totalMl / goalMl * 100 : 0;
                var goalAchieved = totalMl >= goalMl;

                // Map intakes to DTOs
                var intakeDtos = dayIntakes.Select(w => new WaterIntakeDto
                {
                    Id = w.Id,
                    AmountMl = w.AmountMl,
                    ConsumedAt = w.ConsumedAt,
                    Notes = w.Notes
                }).ToList();

                var historyDto = new HistoryDto
                {
                    Date = date.ToDateTime(TimeOnly.MinValue),
                    TotalMl = totalMl,
                    GoalMl = goalMl,
                    ProgressPercentage = progressPercentage,
                    GoalAchieved = goalAchieved,
                    Intakes = intakeDtos
                };

                historyList.Add(historyDto);
            }

            return Result<IEnumerable<HistoryDto>>.Success(historyList);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<HistoryDto>>.Failure(ex, "An error occurred while retrieving water intake history");
        }
    }



    public async Task<Result<UserStatsDto>> GetUserStatsAsync(int userId)
    {
        // TODO: Implement user stats
        return Result<UserStatsDto>.Success(new UserStatsDto());
    }

    private static WaterIntakeDto MapToDto(WaterIntake waterIntake)
    {
        return new WaterIntakeDto
        {
            Id = waterIntake.Id,
            AmountMl = waterIntake.AmountMl,
            ConsumedAt = waterIntake.ConsumedAt,
            Notes = waterIntake.Notes
        };
    }
}
