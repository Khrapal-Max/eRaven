//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MainLayout
//-----------------------------------------------------------------------------

using eRaven.Presentation.Errors;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace eRaven.Components.Layout;

public partial class MainLayout : IDisposable
{
    private ErrorBoundary? _boundary;

    [Inject] private ErrorBoundaryHub ErrorHub { get; set; } = default!;

    protected override void OnInitialized()
    {
        ErrorHub.RecoverRequested += OnRecoverRequested;
    }

    private void TryRecover()
    {
        _boundary?.Recover();
        StateHasChanged();
    }

    private void OnRecoverRequested()
    {
        _boundary?.Recover();
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ErrorHub.RecoverRequested -= OnRecoverRequested;
        GC.SuppressFinalize(this);
    }
}
