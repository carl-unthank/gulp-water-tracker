using Gulp.Shared.DTOs;

namespace Gulp.Client.Services;

public interface IApiClient
{
    // Authentication
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<bool> LogoutAsync();
    Task<AuthResponseDto> RefreshTokenAsync();
    Task<UserDto?> GetCurrentUserAsync();

    // Water Intakes
    Task<List<WaterIntakeDto>> GetWaterIntakesAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);
    Task<List<WaterIntakeDto>> GetTodayIntakesAsync();
    Task<WaterIntakeDto?> GetWaterIntakeAsync(int id);
    Task<WaterIntakeDto> CreateWaterIntakeAsync(CreateWaterIntakeDto createDto);
    Task<WaterIntakeDto> UpdateWaterIntakeAsync(int id, UpdateWaterIntakeDto updateDto);
    Task<bool> DeleteWaterIntakeAsync(int id);
    Task<int> GetTotalForDateAsync(DateTime date);
    Task<Dictionary<DateTime, int>> GetDailyTotalsAsync(DateTime startDate, DateTime endDate);
    Task<List<QuickAmountDto>> GetQuickAmountsAsync();

    // Daily Goals
    Task<DailyGoalDto?> GetCurrentGoalAsync();
    Task<DailyGoalDto?> GetGoalForDateAsync(DateTime date);
    Task<List<DailyGoalDto>> GetGoalHistoryAsync();
    Task<DailyGoalDto> CreateDailyGoalAsync(DailyGoalDto createDto);
    Task<DailyGoalDto> UpdateCurrentGoalAsync(DailyGoalDto updateDto);
    Task<bool> DeleteDailyGoalAsync(int id);

    // Dashboard
    Task<DashboardDto> GetDashboardAsync();
    Task<InsightsDto> GetInsightsAsync();
    Task<List<HistoryDto>> GetHistoryAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 30);
    Task<List<DailyProgressDto>> GetDailyProgressAsync(DateTime startDate, DateTime endDate);
}
