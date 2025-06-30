using Gulp.Shared.DTOs;

namespace Gulp.Client.Services.State;

public interface IStateService
{
    AppState State { get; }
    
    // User Actions
    Task<bool> LoginAsync(LoginDto loginDto);
    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task InitializeUserAsync();
    
    // Water Intake Actions
    Task LoadTodayIntakesAsync();
    Task<WaterIntakeDto?> AddIntakeAsync(CreateWaterIntakeDto createDto);
    Task<WaterIntakeDto?> UpdateIntakeAsync(int id, UpdateWaterIntakeDto updateDto);
    Task<bool> DeleteIntakeAsync(int id);
    
    // Daily Goal Actions
    Task LoadCurrentGoalAsync();
    Task<DailyGoalDto?> CreateGoalAsync(DailyGoalDto createDto);
    Task<DailyGoalDto?> UpdateGoalAsync(DailyGoalDto updateDto);

    // Progress and History Actions
    Task<List<DailyProgressDto>> GetDailyProgressAsync(DateTime startDate, DateTime endDate);
    Task<List<HistoryDto>> GetHistoryAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 30);

    // Utility Actions
    void ClearError();
    void ShowSuccess(string message);
    void ShowError(string message);
}

public class StateService : IStateService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<StateService> _logger;

    public AppState State { get; } = new();

    public StateService(IApiClient apiClient, ILogger<StateService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // User Actions
    public async Task<bool> LoginAsync(LoginDto loginDto)
    {
        try
        {
            State.SetLoading(true);
            State.ClearMessages();

            var response = await _apiClient.LoginAsync(loginDto);
            if (response.Success && response.User != null)
            {
                State.SetUser(response.User);
                State.SetSuccess("Login successful!");
                
                // Load initial data
                await Task.WhenAll(
                    LoadTodayIntakesAsync(),
                    LoadCurrentGoalAsync()
                );
                
                return true;
            }
            else
            {
                State.SetError(response.Message ?? "Login failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            State.SetError("An error occurred during login");
            return false;
        }
        finally
        {
            State.SetLoading(false);
        }
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            State.SetLoading(true);
            State.ClearMessages();

            var response = await _apiClient.RegisterAsync(registerDto);
            if (response.Success && response.User != null)
            {
                State.SetUser(response.User);
                State.SetSuccess("Registration successful!");
                
                // Load initial data
                await Task.WhenAll(
                    LoadTodayIntakesAsync(),
                    LoadCurrentGoalAsync()
                );
                
                return true;
            }
            else
            {
                State.SetError(response.Message ?? "Registration failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            State.SetError("An error occurred during registration");
            return false;
        }
        finally
        {
            State.SetLoading(false);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        finally
        {
            State.Logout();
        }
    }

    public async Task InitializeUserAsync()
    {
        try
        {
            State.SetLoading(true);
            var user = await _apiClient.GetCurrentUserAsync();
            
            if (user != null)
            {
                State.SetUser(user);
                
                // Load initial data
                await Task.WhenAll(
                    LoadTodayIntakesAsync(),
                    LoadCurrentGoalAsync()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing user");
        }
        finally
        {
            State.SetLoading(false);
        }
    }

    // Water Intake Actions
    public async Task LoadTodayIntakesAsync()
    {
        try
        {
            State.SetLoadingIntakes(true);
            var intakes = await _apiClient.GetTodayIntakesAsync();
            State.SetTodayIntakes(intakes?.ToList() ?? new List<WaterIntakeDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading today's intakes");
            State.SetError("Failed to load water intakes");
        }
        finally
        {
            State.SetLoadingIntakes(false);
        }
    }

    public async Task<WaterIntakeDto?> AddIntakeAsync(CreateWaterIntakeDto createDto)
    {
        try
        {
            var intake = await _apiClient.CreateWaterIntakeAsync(createDto);
            State.AddIntake(intake);
            State.SetSuccess($"Added {intake.AmountMl}ml water intake!");
            return intake;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding water intake");
            State.SetError("Failed to add water intake");
            return null;
        }
    }

    public async Task<WaterIntakeDto?> UpdateIntakeAsync(int id, UpdateWaterIntakeDto updateDto)
    {
        try
        {
            var intake = await _apiClient.UpdateWaterIntakeAsync(id, updateDto);
            State.UpdateIntake(intake);
            State.SetSuccess("Water intake updated!");
            return intake;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating water intake");
            State.SetError("Failed to update water intake");
            return null;
        }
    }

    public async Task<bool> DeleteIntakeAsync(int id)
    {
        try
        {
            await _apiClient.DeleteWaterIntakeAsync(id);
            State.RemoveIntake(id);
            State.SetSuccess("Water intake deleted!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting water intake");
            State.SetError("Failed to delete water intake");
            return false;
        }
    }

    // Daily Goal Actions
    public async Task LoadCurrentGoalAsync()
    {
        try
        {
            State.SetLoadingGoal(true);
            var goal = await _apiClient.GetCurrentGoalAsync();
            State.SetCurrentGoal(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading current goal");
            State.SetError("Failed to load daily goal");
        }
        finally
        {
            State.SetLoadingGoal(false);
        }
    }

    public async Task<DailyGoalDto?> CreateGoalAsync(DailyGoalDto createDto)
    {
        try
        {
            var goal = await _apiClient.CreateDailyGoalAsync(createDto);
            State.SetCurrentGoal(goal);
            State.SetSuccess($"Daily goal set to {goal.TargetAmountMl}ml!");
            return goal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating daily goal");
            State.SetError("Failed to create daily goal");
            return null;
        }
    }

    public async Task<DailyGoalDto?> UpdateGoalAsync(DailyGoalDto updateDto)
    {
        try
        {
            var goal = await _apiClient.UpdateCurrentGoalAsync(updateDto);
            State.SetCurrentGoal(goal);
            State.SetSuccess($"Daily goal updated to {goal.TargetAmountMl}ml!");
            return goal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating daily goal");
            State.SetError("Failed to update daily goal");
            return null;
        }
    }

    // Progress and History Actions
    public async Task<List<DailyProgressDto>> GetDailyProgressAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await _apiClient.GetDailyProgressAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading daily progress data");
            State.SetError("Failed to load progress data");
            return new List<DailyProgressDto>();
        }
    }

    public async Task<List<HistoryDto>> GetHistoryAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 30)
    {
        try
        {
            return await _apiClient.GetHistoryAsync(startDate, endDate, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading history data");
            State.SetError("Failed to load history data");
            return new List<HistoryDto>();
        }
    }

    // Utility Actions
    public void ClearError() => State.ClearMessages();
    public void ShowSuccess(string message) => State.SetSuccess(message);
    public void ShowError(string message) => State.SetError(message);
}
