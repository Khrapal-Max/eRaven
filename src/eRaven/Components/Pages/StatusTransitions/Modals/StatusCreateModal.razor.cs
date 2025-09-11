//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions
//-----------------------------------------------------------------------------

using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.StatusTransitions.Modals;

public partial class StatusCreateModal : ComponentBase
{
    // =================== UI ====================
    public bool IsOpen { get; private set; }
    public bool Busy { get; private set; }
    public CreateKindViewModel Model { get; private set; } = new();

    // =================== Logic ====================
    [Inject] private IStatusKindService KindService { get; set; } = default!;
    [Parameter] public EventCallback<StatusKind> OnCreated { get; set; }

    private async Task CreateAsync()
    {
        if (Busy) return;
        try
        {
            Busy = true;

            var created = await KindService.CreateAsync(new CreateKindViewModel
            {
                Name = Model.Name.Trim(),
                Code = Model.Code.Trim(),
                Order = Model.Order,
                IsActive = Model.IsActive
            });

            await OnCreated.InvokeAsync(created);
            Close();
        }
        catch
        {
            // помилки покажете тостами у батька (або додайте Toast тут)
            Close();
        }
        finally
        {
            Busy = false;
        }
    }

    // =================== API ====================

    public void Open()
    {
        if (Busy) return;
        Model = new();
        IsOpen = true;
        StateHasChanged();
    }

    private void Close() { IsOpen = false; StateHasChanged(); }
}
