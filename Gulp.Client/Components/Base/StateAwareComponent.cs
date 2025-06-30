using Microsoft.AspNetCore.Components;
using Gulp.Client.Services.State;

namespace Gulp.Client.Components.Base;

public abstract class StateAwareComponent : ComponentBase, IDisposable
{
    [Inject] protected IStateService StateService { get; set; } = null!;
    
    protected AppState State => StateService.State;

    protected override void OnInitialized()
    {
        // Subscribe to state changes
        State.OnChange += StateHasChanged;
        base.OnInitialized();
    }

    protected virtual void OnStateChanged()
    {
        // Override this method in derived components to handle specific state changes
    }

    public void Dispose()
    {
        // Unsubscribe from state changes
        State.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
