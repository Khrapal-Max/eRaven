//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Pages.Plans.Modals;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanActionsPanel : ComponentBase
{
    [Parameter] public Guid PlanId { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;

    private readonly List<PlanActionViewModel> _actions = [];
    private PlanActionViewModel? _selected;
    private bool _loading = true;

    private CreatePlanActionModal? _createModal;

    protected override async Task OnParametersSetAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _actions.Clear();
        var data = await PlanActionService.GetByPlanAsync(PlanId);
        _actions.AddRange(data.OrderBy(x => x.EventAtUtc));
        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private void OpenCreate() => _createModal?.Open();

    private async Task HandleCreated(PlanActionViewModel created)
    {
        await ReloadAsync();
    }

    private Task OnSelectedChanged(PlanActionViewModel? vm)
    {
        _selected = vm;
        return Task.CompletedTask;
    }

    private async Task DeleteAsync(Guid actionId)
    {
        if (ReadOnly) return;
        var ok = await PlanActionService.DeleteAsync(actionId);
        if (ok) await ReloadAsync();
    }
}
