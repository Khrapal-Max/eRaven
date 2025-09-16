// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// OrderDetails
// -----------------------------------------------------------------------------

using eRaven.Application.Services.OrderService;
using eRaven.Application.ViewModels.OrderViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Orders;

public partial class OrderDetails : ComponentBase
{
    [Parameter] public Guid Id { get; set; }
    [Inject] public IOrderService OrderService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private OrderDetailsViewModel? _details;
    private bool _loading = true;

    // для TableBaseComponent
    private OrderActionViewModel? _selectedAction;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _details = await OrderService.GetByIdAsync(Id);
        _loading = false;
    }

    private Task OnSelectedActionChanged(OrderActionViewModel? a)
    {
        _selectedAction = a;
        return Task.CompletedTask;
    }

    private void OnActionRowClick(OrderActionViewModel a)
    {
        // Перехід у план, з якого підтверджена дія
        Nav.NavigateTo($"/plans/{a.PlanId}");
    }
}