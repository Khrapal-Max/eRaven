// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// OrderPage
// -----------------------------------------------------------------------------

using eRaven.Application.Services.OrderService;
using eRaven.Application.ViewModels.OrderViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Orders;

public partial class OrderPage : ComponentBase
{
    [Inject] public IOrderService OrderService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private readonly List<OrderViewModel> _orders = [];
    private OrderViewModel? _selected;

    private bool _busy;
    private bool _loading = true;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        var result = await OrderService.GetAllOrderAsync();
        _orders.Clear();
        _orders.AddRange(result.OrderByDescending(o => o.RecordedUtc));
        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteAsync(Guid id)
    {
        _busy = true;
        try
        {
            var ok = await OrderService.DeleteAsync(id);
            if (ok)
            {
                _orders.RemoveAll(x => x.Id == id);
                if (_selected?.Id == id) _selected = null;
            }
        }
        finally { _busy = false; }
    }

    // ---- Табличні хендлери ----
    private Task OnSelectedChanged(OrderViewModel? item)
    {
        _selected = item;
        return Task.CompletedTask;
    }

    private void OnRowClick(OrderViewModel item)
    {
        // перехід у деталі по кліку на рядку
        Nav.NavigateTo($"/orders/{item.Id}");
    }
}
