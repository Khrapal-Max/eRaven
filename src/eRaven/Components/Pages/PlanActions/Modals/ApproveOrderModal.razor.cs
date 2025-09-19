//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApproveOrderModal
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class ApproveOrderModal
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;

    [Parameter] public HashSet<Guid> SelectedIds { get; set; } = new();
    [Parameter] public EventCallback OnApproved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private ApproveOptionsViewModel Vm = new();

    private async Task ApproveAsync()
    {
        var res = await PlanActionService.ApproveBatchAsync(SelectedIds, new ApproveOptions(Vm.OrderName, Vm.Author));
        // (опційно) показати помилки res.PerAction де Applied=false
        await OnApproved.InvokeAsync();
    }
}
