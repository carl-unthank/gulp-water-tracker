using Gulp.Shared.DTOs;
using Gulp.Shared.Common;

namespace Gulp.Infrastructure.Interfaces;

public interface IDailyGoalService
{
    Task<Result<DailyGoalDto>> GetCurrentDailyGoalAsync(int userId);
    Task<Result<DailyGoalDto>> GetDailyGoalForDateAsync(int userId, DateTime date);
    Task<Result<DailyGoalDto>> UpdateDailyGoalAsync(int userId, DailyGoalDto updateDto);
    Task<Result<IEnumerable<DailyGoalDto>>> GetDailyGoalHistoryAsync(int userId, int page, int pageSize);
}
