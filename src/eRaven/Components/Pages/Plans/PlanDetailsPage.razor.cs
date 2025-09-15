// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanDetailsPage
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Components.Pages.Plans.Modals;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Enums;
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
    private Guid? _preselectedPersonId;
    private bool _loaded;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            _plan = await PlanService.GetPlanAsync(Id, _cts.Token);
            if (_plan is null) return;

            _participants = [.. await PlanService.GetPlanParticipantsAsync(_plan.Id, _cts.Token)];
            _actions = [.. await PlanService.GetPlanActionsAsync(_plan.Id, _cts.Token)];
            _loaded = true;               // ← позначаємо, що завантаження закінчено
        }
        catch (Exception ex)
        {
            Toasts.ShowError("Не вдалося завантажити деталі плану. " + ex.Message);
            _loaded = false;
        }
        finally
        {
            Busy = false;                 // ← обов’язково повернути Busy в false
            StateHasChanged();
        }
    }

    private void Back() => Nav.NavigateTo("/plans");

    private async Task OpenAddAction() => await OpenAddActionCore(null);
    private async Task OpenAddAction(Guid? personId) => await OpenAddActionCore(personId);

    private async Task OpenAddActionCore(Guid? personId)
    {
        if (_plan is null || _addActionModal is null) return;

        // 1) Покласти значення у поле, яке зв’язане з параметром PreselectedPersonId
        _preselectedPersonId = personId;

        // 2) Попросити Blazor оновити розмітку, щоб параметри доїхали у дочірній компонент
        await InvokeAsync(StateHasChanged);

        // 3) Відкрити модалку — всередині вона прочитає вже актуальні параметри
        await _addActionModal.OpenAsync();
    }

    private async Task OpenAddBatch()
    {
       /* if (_plan is null || _addBatchModal is null) return;

        await _addBatchModal.OpenAsync(new AddBatchModal.Context
        {
            PlanNumber = _plan.PlanNumber
        });*/
    }

    private async Task ClosePlan()
    {
        if (_plan is null) return;

        var ok = await _confirm.ShowConfirmAsync($"Закрити план «{_plan.PlanNumber}»? Подальші дії стануть недоступними.");
        if (!ok) return;

        try
        {
            SetBusy(true);
            var result = await PlanService.ClosePlanAsync(_plan.Id, author: "ui", _cts.Token);
            if (result)
            {
                Toasts.ShowSuccess("План закрито.");
                await ReloadAsync();
            }
            else
            {
                Toasts.ShowWarning("План не знайдено.");
            }
        }
        catch (Exception ex)
        {
            Toasts.ShowError("Не вдалося закрити план. " + ex.Message);
        }
        finally { SetBusy(false); }
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
    private bool CanEdit =>
    _loaded && !Busy && _plan is not null && _plan.State == PlanState.Open;
    public void Dispose() { _cts.Cancel(); _cts.Dispose(); GC.SuppressFinalize(this); }
}