/*// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlansPage
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Components.Pages.Plans;

public partial class PlansPage : ComponentBase, IDisposable
{
    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private List<Plan> _all = [];
    private IReadOnlyList<Plan> _view = [];

    private bool _createOpen;
    private ConfirmModal _confirm = default!;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);
            _all = [.. await PlanService.GetAllPlansAsync(_cts.Token)];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Не вдалося завантажити плани. " + ex.Message);
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

    private async Task CreatePlanAsync(string planNumber)
    {
        try
        {
            Busy = true;

            var vm = new CreatePlanViewModel
            {
                PlanNumber = (planNumber ?? string.Empty).Trim(),
                State = PlanState.Open
            };

            var created = await PlanService.EnsurePlanAsync(vm, author: "ui", _cts.Token);

            _createOpen = false;
            ToastService.ShowSuccess("План створено.");
            Navigation.NavigateTo($"/plans/{created.Id:D}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Не вдалося створити план. " + ex.Message);
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    private async Task DeletePlanAsync(Plan p)
    {
        if (p.State != PlanState.Open)
        {
            ToastService.ShowWarning("План закритий — видалення заборонено.");
            return;
        }

        var ok = await _confirm.ShowConfirmAsync($"Видалити план «{p.PlanNumber}»? Дію неможливо скасувати.");
        if (!ok) return;

        try
        {
            SetBusy(true);
            var deleted = await PlanService.DeletePlanAsync(p.Id, _cts.Token);
            if (deleted)
            {
                ToastService.ShowSuccess("План видалено.");
                await ReloadAsync();
            }
            else
            {
                ToastService.ShowWarning("План уже видалено або не знайдено.");
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Не вдалося видалити план. " + ex.Message);
        }
        finally { SetBusy(false); }
    }

    private void OpenCreateModal() { _createOpen = true; StateHasChanged(); }
    private void CloseCreateModal() { _createOpen = false; StateHasChanged(); }

    private void SetBusy(bool v) { Busy = v; StateHasChanged(); }
    public void Dispose() { _cts.Cancel(); _cts.Dispose(); GC.SuppressFinalize(this); }
}*/