//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusHistoryModal (code-behind)
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusService;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Modals;

public sealed partial class PersonStatusHistoryModal : ComponentBase
{
    // Керування з батька
    [Parameter] public bool Open { get; set; }
    [Parameter] public Person? Person { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!;

    private readonly List<PersonStatus> _view = [];
    private bool _busy;
    private string? _personName;

    protected override async Task OnParametersSetAsync()
    {
        if (Person is not null)
        {
            _personName = Person?.FullName;

            // Кожен раз, коли модал відкривають і відомий PersonId — оновлюємо список
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            SetBusy(true);

            // Беремо всю історію і лишаємо валідні записи (IsActive = true)
            var all = await PersonStatusService.GetHistoryAsync(Person!.Id);
            _view.Clear();
            _view.AddRange(all);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        _busy = value;
        StateHasChanged();
    }

    private async Task CloseAsync()
        => await OnClose.InvokeAsync();
}
