//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentHistoryModal (code-behind)
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Modals;

public sealed partial class PositionAssignmentHistoryModal : ComponentBase
{
    // Керування з батька
    [Parameter] public bool Open { get; set; }
    [Parameter] public Person? Person { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Inject] private IPositionAssignmentService PositionAssignmentService { get; set; } = default!;

    private readonly List<PersonPositionAssignment> _view = [];
    private bool _busy;
    private string? _personName;

    protected override async Task OnParametersSetAsync()
    {
        if (Open && Person is not null)
        {
            _personName = Person.FullName;
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            SetBusy(true);

            // Сервіс вже повертає включно з PositionUnit, відсортовано по OpenUtc ↓ (див. реалізацію)
            var hist = await PositionAssignmentService.GetHistoryAsync(Person!.Id, limit: 200);
            _view.Clear();
            _view.AddRange(hist);
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
