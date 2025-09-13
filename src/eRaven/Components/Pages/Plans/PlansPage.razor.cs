// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlansPage
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlansPage : ComponentBase, IDisposable
{
    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private List<Plan> _all = [];
    private IReadOnlyList<Plan> _view = [];

    private bool _createOpen;
    private string? _newPlanNumber;

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
            Toast.ShowError("Не вдалося завантажити плани. " + ex.Message);
            _all.Clear();
            _view = [];
        }
        finally { SetBusy(false); }
    }

    protected Task OnSearchAsync() { ApplyFilter(); return Task.CompletedTask; }

    private void ApplyFilter()
    {
        var s = (Search ?? string.Empty).Trim();
        IEnumerable<Plan> q = _all;

        if (s.Length > 0)
            q = q.Where(p => p.PlanNumber.Contains(s, StringComparison.OrdinalIgnoreCase));

        _view = q.OrderByDescending(p => p.RecordedUtc).ToList().AsReadOnly();
        StateHasChanged();
    }

    private async Task CreatePlanAsync()
    {
        if (string.IsNullOrWhiteSpace(_newPlanNumber)) return;

        try
        {
            SetBusy(true);
            var vm = new CreatePlanViewModel { PlanNumber = _newPlanNumber.Trim(), State = PlanState.Open };
            var created = await PlanService.CreateAsync(vm, _cts.Token);
            _createOpen = false;
            Toast.ShowSuccess("План створено.");
            Nav.NavigateTo($"/plans/{created.Id:D}");
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося створити план. " + ex.Message);
        }
        finally { SetBusy(false); }
    }

    private async Task DeletePlanAsync(Plan p)
    {
        if (p.State != PlanState.Open) { Toast.ShowWarning("План закритий — видалення заборонено."); return; }
        try
        {
            SetBusy(true);
            var ok = await PlanService.DeleteIfOpenAsync(p.Id, _cts.Token);
            if (ok) Toast.ShowSuccess("План видалено.");
            await ReloadAsync();
        }
        catch (Exception ex) { Toast.ShowError("Не вдалося видалити план. " + ex.Message); }
        finally { SetBusy(false); }
    }

    private void OpenCreateModal() => _createOpen = true;
    private void CloseCreateModal() { _createOpen = false; _newPlanNumber = null; }

    private void SetBusy(bool v) { Busy = v; StateHasChanged(); }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
