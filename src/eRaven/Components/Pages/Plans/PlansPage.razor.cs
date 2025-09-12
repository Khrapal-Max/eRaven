// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlansPage (code-behind)
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlansPage : ComponentBase, IDisposable
{
    // -------------------- State --------------------
    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private List<Plan> _all = [];
    private IReadOnlyList<Plan> _view = [];

    // Create modal
    private bool _createOpen;

    // Delete plan
    private ConfirmModal _confirm = default!;

    // -------------------- DI --------------------
    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;  

    // -------------------- Lifecycle --------------------
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);
            var items = await PlanService.GetAllPlansAsync(_cts.Token);
            _all = [.. items];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Не вдалося завантажити плани: {ex.Message}");
            _all.Clear();
            _view = [];
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -------------------- Search --------------------
    protected Task OnSearchAsync()
    {
        ApplyFilter();
        return Task.CompletedTask;
    }

    private void ApplyFilter()
    {
        var s = (Search ?? string.Empty).Trim();
        IEnumerable<Plan> query = _all;

        if (!string.IsNullOrWhiteSpace(s))
        {
            query = query.Where(p => p.PlanNumber.Contains(s));
        }

        _view = query
            .OrderByDescending(p => p.RecordedUtc)
            .ToList()
            .AsReadOnly();

        StateHasChanged();
    }

    // -------------------- Actions palns --------------------

    private async Task CreatePlanAsync(string planNumber)
    {
        try
        {
            SetBusy(true);

            // Створюємо ПОРОЖНІЙ план (без елементів), далі переходимо у деталізацію
            var vm = new CreatePlanViewModel
            {
                PlanNumber = (planNumber ?? string.Empty).Trim(),
                State = PlanState.Open,
                PlanElements = [] // важливо: пустий список = дозволено
            };

            var created = await PlanService.CreateAsync(vm, _cts.Token);

            _createOpen = false;
            ToastService.ShowSuccess("План створено.");

            Navigation.NavigateTo(PlanDetailsHref(created.Id));
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Не вдалося створити план. " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task DeletePlanAsync(Plan p)
    {
        if (p.State != PlanState.Open) { ToastService.ShowWarning("План закритий — видалення заборонено."); return; }
        var ok = await _confirm.ShowConfirmAsync($"Видалити план {p.PlanNumber}?");
        if (!ok) return;

        try
        {
            SetBusy(true);
            var res = await PlanService.DeleteIfOpenAsync(p.Id, _cts.Token);

            ToastService.ShowSuccess("План видалено.");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Не вдалося видалити план. " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -------------------- Create (modal) --------------------
    private void OpenCreateModal() => _createOpen = true;
    private Task CloseCreateModal() { _createOpen = false; return Task.CompletedTask; }    

    // -------------------- Navigation --------------------
    private static string PlanDetailsHref(Guid id) => $"/plans/{id:D}";

    // -------------------- Utils --------------------
    private void SetBusy(bool value) { Busy = value; StateHasChanged(); }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
