using Gulp.Shared.DTOs;

namespace Gulp.Client.Services;

public class WaterIntakeNotificationService
{
    public event Action<CreateWaterIntakeDto>? WaterIntakeAdded;
    public event Action? WaterIntakeUpdated;
    public event Action? GoalUpdated;
    public event Action? ShowAddWaterModal;
    public event Action? ShowSetGoalModal;

    public void NotifyWaterIntakeAdded(CreateWaterIntakeDto createDto)
    {
        WaterIntakeAdded?.Invoke(createDto);
    }

    public void NotifyWaterIntakeUpdated()
    {
        WaterIntakeUpdated?.Invoke();
    }

    public void NotifyGoalUpdated()
    {
        GoalUpdated?.Invoke();
    }

    public void RequestShowAddWaterModal()
    {
        ShowAddWaterModal?.Invoke();
    }

    public void RequestShowSetGoalModal()
    {
        ShowSetGoalModal?.Invoke();
    }
}
