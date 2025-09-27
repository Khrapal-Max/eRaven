//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions
//-----------------------------------------------------------------------------

using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StatusKindViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.StatusTransitions.Modals;

public partial class StatusOrderEditModal : ComponentBase
{
    // ---------- UI state ----------
    public bool IsOpen { get; private set; }
    public bool Busy { get; private set; }
    public EditKindOrderViewModel Model { get; private set; } = new();

    // ---------- Logic ----------
    [Inject] private IStatusKindService KindService { get; set; } = default!;

    // Повертаємо наверх ідентифікатор та нове значення
    [Parameter] public EventCallback<(int Id, int NewOrder)> OnSaved { get; set; }

    public void Open(int id, string name, int currentOrder)
    {
        if (Busy) return;
        Model = new EditKindOrderViewModel
        {
            Id = id,
            Name = name,
            CurrentOrder = currentOrder,
            NewOrder = currentOrder
        };
        IsOpen = true;
        StateHasChanged();
    }

    private void Close()
    {
        if (Busy) return;
        IsOpen = false;
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        if (Busy) return;

        // Немає змін — просто закриваємо
        if (Model.NewOrder == Model.CurrentOrder)
        {
            Close();
            return;
        }

        try
        {
            Busy = true;
            var ok = await KindService.UpdateOrderAsync(Model.Id, Model.NewOrder);
            if (ok)
            {
                await OnSaved.InvokeAsync((Model.Id, Model.NewOrder));
            }
        }
        finally
        {
            Busy = false;
            Close();
        }
    }
}
