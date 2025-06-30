using Gulp.Shared.DTOs;

namespace Gulp.Client.Services.State;

public class AppState
{
    // User State
    public UserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsLoading { get; private set; }

    // Water Intake State
    public List<WaterIntakeDto> TodayIntakes { get; private set; } = new();
    public int TotalTodayAmount => TodayIntakes.Sum(i => i.AmountMl);
    public bool IsLoadingIntakes { get; private set; }

    // Daily Goal State
    public DailyGoalDto? CurrentGoal { get; private set; }
    public bool IsLoadingGoal { get; private set; }

    // UI State
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    // Events for state changes
    public event Action? OnChange;
    public event Action<UserDto?>? OnUserChanged;
    public event Action<List<WaterIntakeDto>>? OnIntakesChanged;
    public event Action<DailyGoalDto?>? OnGoalChanged;
    public event Action<string?>? OnErrorChanged;

    // User Actions
    public void SetUser(UserDto? user)
    {
        CurrentUser = user;
        IsAuthenticated = user != null;
        NotifyStateChanged();
        OnUserChanged?.Invoke(user);
    }

    public void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        IsAuthenticated = false;
        TodayIntakes.Clear();
        CurrentGoal = null;
        ClearMessages();
        NotifyStateChanged();
        OnUserChanged?.Invoke(null);
    }

    // Water Intake Actions
    public void SetTodayIntakes(List<WaterIntakeDto> intakes)
    {
        TodayIntakes = intakes ?? new List<WaterIntakeDto>();
        NotifyStateChanged();
        OnIntakesChanged?.Invoke(TodayIntakes);
    }

    public void AddIntake(WaterIntakeDto intake)
    {
        TodayIntakes.Add(intake);
        TodayIntakes = TodayIntakes.OrderByDescending(i => i.ConsumedAt).ToList();
        NotifyStateChanged();
        OnIntakesChanged?.Invoke(TodayIntakes);
    }

    public void UpdateIntake(WaterIntakeDto updatedIntake)
    {
        var index = TodayIntakes.FindIndex(i => i.Id == updatedIntake.Id);
        if (index >= 0)
        {
            TodayIntakes[index] = updatedIntake;
            TodayIntakes = TodayIntakes.OrderByDescending(i => i.ConsumedAt).ToList();
            NotifyStateChanged();
            OnIntakesChanged?.Invoke(TodayIntakes);
        }
    }

    public void RemoveIntake(int intakeId)
    {
        TodayIntakes.RemoveAll(i => i.Id == intakeId);
        NotifyStateChanged();
        OnIntakesChanged?.Invoke(TodayIntakes);
    }

    public void SetLoadingIntakes(bool isLoading)
    {
        IsLoadingIntakes = isLoading;
        NotifyStateChanged();
    }

    // Daily Goal Actions
    public void SetCurrentGoal(DailyGoalDto? goal)
    {
        CurrentGoal = goal;
        NotifyStateChanged();
        OnGoalChanged?.Invoke(goal);
    }

    public void SetLoadingGoal(bool isLoading)
    {
        IsLoadingGoal = isLoading;
        NotifyStateChanged();
    }

    // Message Actions
    public void SetError(string? message)
    {
        ErrorMessage = message;
        SuccessMessage = null; // Clear success when showing error
        NotifyStateChanged();
        OnErrorChanged?.Invoke(message);
    }

    public void SetSuccess(string? message)
    {
        SuccessMessage = message;
        ErrorMessage = null; // Clear error when showing success
        NotifyStateChanged();
    }

    public void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        NotifyStateChanged();
    }

    // Progress Calculations
    public double GetProgressPercentage()
    {
        if (CurrentGoal?.TargetAmountMl <= 0) return 0;
        return Math.Min(100, (double)TotalTodayAmount / CurrentGoal!.TargetAmountMl * 100);
    }

    public int GetRemainingAmount()
    {
        if (CurrentGoal?.TargetAmountMl <= 0) return 0;
        return Math.Max(0, CurrentGoal!.TargetAmountMl - TotalTodayAmount);
    }

    public bool IsGoalReached()
    {
        return CurrentGoal?.TargetAmountMl > 0 && TotalTodayAmount >= CurrentGoal.TargetAmountMl;
    }

    // Computed Properties
    public int IntakeCount => TodayIntakes.Count;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
