//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Statuses.Modals;

public partial class StatusSetModal : ComponentBase
{
    [Parameter] public EventCallback<Guid> OnStatusChanged { get; set; }

    [Inject] private IToastService Toast { get; set; } = default!;

    private Person? _person;
    private bool _open;

    // Відкриває модалку та передає вибрану особу
    public void Open(Person person)
    {
        _person = person;
        _open = true;
        StateHasChanged();
    }

    // Підтвердження (поки — заглушка + нотифікація)
    private async Task SaveAsync()
    {
        Toast.ShowInfo("В розробці: зміна статусу");

        _open = false;
        if (_person is not null)
            await OnStatusChanged.InvokeAsync(_person.Id);
    }

    // Закрити без дій
    private void Cancel()
    {
        _open = false;
        StateHasChanged();
    }
}
