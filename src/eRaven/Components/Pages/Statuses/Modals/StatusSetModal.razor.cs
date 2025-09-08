//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionsPage
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Statuses.Modals;

public partial class StatusSetModal : ComponentBase
{
    [Parameter] public EventCallback<Guid> OnStatusChanged { get; set; }

    private Person? _person;
    private bool _open;

    public void Open(Person person)
    {
        _person = person;
        _open = true;
        StateHasChanged();
    }

    private async Task ConfirmAsync()
    {
        // TODO: викликати реальний сервіс зміни статусу
        // Поки що просто закриваємо і повідомляємо батьківський компонент
        _open = false;
        await OnStatusChanged.InvokeAsync(_person!.Id);
    }

    private void Close()
    {
        _open = false;
        StateHasChanged();
    }
}
