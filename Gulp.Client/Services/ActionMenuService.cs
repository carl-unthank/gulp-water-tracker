using Gulp.Shared.DTOs;

namespace Gulp.Client.Services;

public class ActionMenuService
{
    private int? _openMenuId = null;

    public event Action? StateChanged;
    public event Action<WaterIntakeDto>? EditRequested;

    public bool IsMenuOpen(int itemId)
    {
        return _openMenuId == itemId;
    }

    public void ToggleMenu(int itemId)
    {
        _openMenuId = _openMenuId == itemId ? null : itemId;
        StateChanged?.Invoke();
    }

    public void CloseAllMenus()
    {
        _openMenuId = null;
        StateChanged?.Invoke();
    }

    public void RequestEdit(WaterIntakeDto intake)
    {
        CloseAllMenus();
        EditRequested?.Invoke(intake);
    }
}
