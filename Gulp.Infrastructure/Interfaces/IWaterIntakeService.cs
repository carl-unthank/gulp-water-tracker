using Gulp.Shared.DTOs;
using Gulp.Shared.Common;

namespace Gulp.Infrastructure.Interfaces;

public interface IWaterIntakeService
{
    Task<Result<WaterIntakeDto>> RecordWaterIntakeAsync(int userId, CreateWaterIntakeDto createDto);
    Task<Result<WaterIntakeDto>> GetWaterIntakeByIdAsync(int id, int userId);
    Task<Result<IEnumerable<WaterIntakeDto>>> GetTodaysWaterIntakeAsync(int userId);
    Task<Result<IEnumerable<WaterIntakeDto>>> GetWaterIntakeByDateAsync(int userId, DateOnly date);
    Task<Result<WaterIntakeDto>> UpdateWaterIntakeAsync(int id, int userId, UpdateWaterIntakeDto updateDto);
    Task<Result> DeleteWaterIntakeAsync(int id, int userId);
    Task<Result<HistoryDto>> GetDailyProgressAsync(int userId);
    Task<Result<IEnumerable<HistoryDto>>> GetWaterIntakeHistoryAsync(int userId, DateOnly startDate, DateOnly endDate, int page, int pageSize);
    Task<Result<UserStatsDto>> GetUserStatsAsync(int userId);
}
