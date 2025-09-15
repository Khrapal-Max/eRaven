// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanDetailsPage
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Components.Pages.Plans.Modals;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanDetailsPage : ComponentBase, IDisposable
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService Toasts { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }

    private Plan? _plan;
    private List<PlanParticipant> _participants = [];
    private List<PlanParticipantAction> _actions = [];

    private ConfirmModal _confirm = default!;
    private AddActionModal _addActionModal = default!;
    private AddBatchModal _addBatchModal = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            _plan = await PlanService.GetPlanAsync(Id, _cts.Token);
            if (_plan is null) return;

            _participants = (await PlanService.GetPlanParticipantsAsync(_plan.Id, _cts.Token)).ToList();
            _actions = (await PlanService.GetPlanActionsAsync(_plan.Id, _cts.Token)).ToList();
        }
        catch (Exception ex)
        {
            Toasts.ShowError("Не вдалося завантажити деталі плану. " + ex.Message);
        }
        finally { SetBusy(false); }
    }

    private void Back() => Nav.NavigateTo("/plans");

    private async Task OpenAddAction()
    {
        if (_plan is null || _addActionModal is null) return;
        await _addActionModal.OpenAsync(_plan.PlanNumber);
    }

    private async Task OpenAddAction(Guid personId)
    {
        if (_plan is null || _addActionModal is null) return;
        await _addActionModal.OpenAsync(_plan.PlanNumber, personId);
    }

    private Task OpenAddBatch()
    {
       /* if (_plan is null || _addBatchModal is null) return;

        await _addBatchModal.OpenAsync(new AddBatchModal.Context
        {
            PlanNumber = _plan.PlanNumber
        });*/

        return Task.CompletedTask;
    }

    private async Task OnActionSaved()
    {
        Toasts.ShowSuccess("Дію додано.");
        await ReloadAsync();
    }

    private async Task OnBatchSaved(int savedCount)
    {
        Toasts.ShowSuccess($"Збережено дій: {savedCount}");
        await ReloadAsync();
    }

    private void SetBusy(bool v) { Busy = v; StateHasChanged(); }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}