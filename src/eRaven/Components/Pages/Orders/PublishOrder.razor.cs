// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PublishOrder
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Mappers;
using eRaven.Application.Services.OrderService;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.OrderViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Orders;

public partial class PublishOrder : ComponentBase
{
    [Inject] public IOrderService OrderService { get; set; } = default!;
    [Inject] public IPlanService PlanService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private CreatePublishDailyOrderViewModel _vm = new();

    private DateTime _effectiveDate = DateTime.UtcNow; // двобайндимо в форму, а потім покладемо в _vm
    private bool _busy;

    private bool _loadingPlans = true;
    private readonly List<PlanViewModel> _openPlans = [];
    private readonly HashSet<Guid> _selectedPlanIds = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadOpenPlansAsync();
        _vm.EffectiveMomentUtc = _effectiveDate = DateTime.UtcNow;
    }

    private async Task LoadOpenPlansAsync()
    {
        _loadingPlans = true;
        _openPlans.Clear();
        var domain = await PlanService.GetAllPlanAsync();
        _openPlans.AddRange(domain
            .ToViewModels()
            .Where(p => p.State == PlanState.Open && p.OrderId is null)
            .OrderByDescending(p => p.RecordedUtc));
        _loadingPlans = false;
    }

    private void TogglePlan(Guid id, bool on)
    {
        if (on) _selectedPlanIds.Add(id);
        else _selectedPlanIds.Remove(id);
    }

    private async Task PublishAsync()
    {
        if (_selectedPlanIds.Count == 0) return;
        if (string.IsNullOrWhiteSpace(_vm.Name))
        {
            ToastService.ShowError("Вкажіть № наказу.");
            return;
        }

        _busy = true;
        try
        {
            _vm = _vm with
            {
                EffectiveMomentUtc = DateTime.SpecifyKind(_effectiveDate, DateTimeKind.Utc),
                PlanIds = [.. _selectedPlanIds],
                IncludePlanActionIds = null // мінімальний UX: підтвердити всі дії
            };

            var result = await OrderService.CreateAsync(_vm);
            Nav.NavigateTo($"/orders/{result.Order.Id}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }
}