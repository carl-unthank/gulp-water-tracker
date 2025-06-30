using Microsoft.Extensions.Logging;
using Gulp.Infrastructure.Interfaces;
using Gulp.Shared.DTOs;
using Gulp.Shared.Common;
using Gulp.Shared.Interfaces;
using Gulp.Shared.Models;

namespace Gulp.Infrastructure.Services;

public class DailyGoalService : IDailyGoalService
{
    private readonly IRepository<DailyGoal> _dailyGoalRepository;
    private readonly ILogger<DailyGoalService> _logger;

    public DailyGoalService(
        IRepository<DailyGoal> dailyGoalRepository,
        ILogger<DailyGoalService> logger)
    {
        _dailyGoalRepository = dailyGoalRepository;
        _logger = logger;
    }

    public async Task<Result<DailyGoalDto>> GetCurrentDailyGoalAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting current daily goal for user {UserId}", userId);
            var goals = await _dailyGoalRepository.FindAsync(g => g.UserId == userId);
            _logger.LogInformation("Found {GoalCount} goals for user {UserId}", goals.Count(), userId);

            var currentGoal = goals
                .OrderByDescending(g => g.EffectiveDate)
                .FirstOrDefault();

            if (currentGoal == null)
            {
                _logger.LogWarning("No daily goal found for user {UserId}", userId);
                return Result<DailyGoalDto>.Failure("No daily goal found", "NOT_FOUND");
            }

            _logger.LogInformation("Current goal for user {UserId}: {TargetAmountMl}ml (ID: {GoalId}, EffectiveDate: {EffectiveDate})",
                userId, currentGoal.TargetAmountMl, currentGoal.Id, currentGoal.EffectiveDate);

            var dto = MapToDto(currentGoal);
            return Result<DailyGoalDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current daily goal for user {UserId}", userId);
            return Result<DailyGoalDto>.Failure(ex, "An error occurred while retrieving daily goal");
        }
    }

    public async Task<Result<DailyGoalDto>> GetDailyGoalForDateAsync(int userId, DateTime date)
    {
        try
        {
            var goals = await _dailyGoalRepository.FindAsync(g => g.UserId == userId);

            // Get the goal that was effective for the given date
            // This is the latest goal with EffectiveDate <= date
            var goalForDate = goals
                .Where(g => g.EffectiveDate.Date <= date.Date)
                .OrderByDescending(g => g.EffectiveDate)
                .FirstOrDefault();

            if (goalForDate == null)
            {
                return Result<DailyGoalDto>.Failure("No daily goal found for date", "NOT_FOUND");
            }

            var dto = MapToDto(goalForDate);
            return Result<DailyGoalDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily goal for date {Date} for user {UserId}", date, userId);
            return Result<DailyGoalDto>.Failure(ex, "An error occurred while retrieving daily goal for date");
        }
    }

    public async Task<Result<DailyGoalDto>> UpdateDailyGoalAsync(int userId, DailyGoalDto updateDto)
    {
        try
        {
            _logger.LogInformation("Updating daily goal for user {UserId} to {TargetAmountMl}ml", userId, updateDto.TargetAmountMl);

            // Check if a goal already exists for today
            var existingGoals = await _dailyGoalRepository.FindAsync(g => g.UserId == userId && g.EffectiveDate.Date == DateTime.Today);
            var existingGoal = existingGoals.FirstOrDefault();

            if (existingGoal != null)
            {
                // Update the existing goal for today
                existingGoal.TargetAmountMl = updateDto.TargetAmountMl;
                existingGoal.UpdatedAt = DateTime.UtcNow;
                var updatedGoal = await _dailyGoalRepository.UpdateAsync(existingGoal);
                _logger.LogInformation("Updated existing daily goal with ID {GoalId} for user {UserId}", updatedGoal.Id, userId);

                var dto = MapToDto(updatedGoal);
                return Result<DailyGoalDto>.Success(dto);
            }
            else
            {
                // Create a new goal for today
                var newGoal = new DailyGoal
                {
                    UserId = userId,
                    EffectiveDate = DateTime.Today,
                    TargetAmountMl = updateDto.TargetAmountMl,
                    CreatedAt = DateTime.UtcNow
                };

                var savedGoal = await _dailyGoalRepository.AddAsync(newGoal);
                _logger.LogInformation("Created new daily goal with ID {GoalId} for user {UserId}", savedGoal.Id, userId);

                var dto = MapToDto(savedGoal);
                return Result<DailyGoalDto>.Success(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating daily goal for user {UserId}", userId);
            return Result<DailyGoalDto>.Failure(ex, "An error occurred while updating daily goal");
        }
    }

    public async Task<Result<IEnumerable<DailyGoalDto>>> GetDailyGoalHistoryAsync(int userId, int page, int pageSize)
    {
        try
        {
            var allGoals = await _dailyGoalRepository.FindAsync(g => g.UserId == userId);
            var goals = allGoals
                .OrderByDescending(g => g.EffectiveDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var dtos = goals.Select(MapToDto).ToList();
            return Result<IEnumerable<DailyGoalDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily goal history for user {UserId}", userId);
            return Result<IEnumerable<DailyGoalDto>>.Failure(ex, "An error occurred while retrieving daily goal history");
        }
    }

    private static DailyGoalDto MapToDto(DailyGoal dailyGoal)
    {
        return new DailyGoalDto
        {
            Id = dailyGoal.Id,
            TargetAmountMl = dailyGoal.TargetAmountMl
        };
    }
}
