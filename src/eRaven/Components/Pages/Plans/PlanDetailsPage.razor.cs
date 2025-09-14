//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanDetailsPage
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanDetailsPage : ComponentBase
{
    // -------- Route / DI --------
    [Parameter] public Guid Id { get; set; }

    [Inject] private IPlanService PlanService { get; set; } = default!;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    // -------- Lifecycle --------


    // -------- Nav / Utils --------
    private void GoBack() => NavigationManager.NavigateTo("/plans");
}
