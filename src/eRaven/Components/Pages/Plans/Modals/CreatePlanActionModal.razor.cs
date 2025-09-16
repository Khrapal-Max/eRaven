//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanActionModal : ComponentBase
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] public IPersonService PersonService { get; set; } = default!;

    [Parameter] public Guid PlanId { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    [Parameter] public EventCallback<PlanActionViewModel> OnCreated { get; set; }

    private bool _open;
    private bool _busy;

    private CreatePlanActionViewModel _model = new();
    private DateTime _eventLocal = DateTime.UtcNow; // використовується для datetime-local
    private string _personQuery = string.Empty;
    private List<PersonLookupViewModel> _personResults = [];
    private PersonLookupViewModel? _selectedPerson;

    public void Open()
    {
        if (ReadOnly) return;
        _model = new() { PlanId = PlanId, EventAtUtc = DateTime.UtcNow };
        _eventLocal = DateTime.UtcNow; // локальне представлення; все одно пишемо у UTC
        _personQuery = string.Empty;
        _personResults.Clear();
        _selectedPerson = null;
        _open = true;
        StateHasChanged();
    }

    private async Task CreateAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            _model.EventAtUtc = DateTime.SpecifyKind(_eventLocal, DateTimeKind.Utc);
            var created = await PlanActionService.CreateAsync(_model);
            await OnCreated.InvokeAsync(created);
            Close();
        }
        finally { _busy = false; }
    }

    private void Close() => _open = false;

    private async Task SearchPersons()
    {
        if (string.IsNullOrWhiteSpace(_personQuery)) { _personResults.Clear(); return; }
        //_personResults = (await PersonService.SearchAsync(_personQuery.Trim(), 10)).ToList();
    }

    private void SelectPerson(PersonLookupViewModel person)
    {
        _selectedPerson = person;
        _model.PersonId = person.Id;
    }
}
