//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ViewPlanActionModal
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class ViewPlanActionModal : ComponentBase
{

    private bool _show;
    private bool IsOpen => _show;
    private PlanAction? Model;

    public void Open(PlanAction action)
    {
        Model = action ?? throw new ArgumentNullException(nameof(action));
        _show = true;
        StateHasChanged();
    }

    private void Close()
    {
        _show = false;
        StateHasChanged();
    }
}
