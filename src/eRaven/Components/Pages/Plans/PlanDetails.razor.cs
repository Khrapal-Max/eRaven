// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanDetails
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanDetails : ComponentBase
{
    [Parameter] public Guid Id { get; set; }
    [Inject] public IPlanService PlanService { get; set; } = default!;

    private PlanViewModel? _plan;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _plan = await PlanService.GetByIdAsync(Id);
        _loading = false;
    }
}
