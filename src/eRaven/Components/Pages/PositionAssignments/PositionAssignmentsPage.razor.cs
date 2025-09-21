//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.Services.PositionService;
using eRaven.Components.Pages.PositionAssignments.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments;

public partial class PositionAssignmentsPage : ComponentBase, IDisposable
{
    [Inject] public IPersonService PersonService { get; set; } = default!;
    [Inject] public IPositionService PositionService { get; set; } = default!;
    [Inject] public IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    protected Person? SelectedPerson { get; set; }
    protected PersonPositionAssignment? ActiveAssign { get; set; }
    protected List<PersonPositionAssignment> History { get; private set; } = [];

    private List<Person> _persons = [];
    private List<Person> _filtered = [];
    private List<PositionUnit> _positions = [];

    private AssignPositionModal? _assignModal;
    private UnassignPositionModal? _unassignModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadPersonsAsync();
        await LoadPositionsAsync();
    }

    private async Task LoadPersonsAsync()
    {
        try
        {
            _persons = [.. await PersonService.SearchAsync(null, _cts.Token)];
            await ApplyFilter();
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private async Task LoadPositionsAsync()
    {
        try
        {
            _positions = [.. await PositionService.GetPositionsAsync(true, _cts.Token)];
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private async Task ApplyFilter()
    {
        SelectedPerson = null;
        ActiveAssign = null;
        History.Clear();

        if (string.IsNullOrWhiteSpace(Search))
            _filtered = [.. _persons];
        else
            _filtered = [.. _persons.Where(p =>
                (p.FullName?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Rnokpp?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Callsign?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false))];

        await InvokeAsync(StateHasChanged);
    }

    protected Task OnSearchAsync() => ApplyFilter();

    protected async Task OnPersonClick(Person p)
    {
        SelectedPerson = p;

        try
        {
            ActiveAssign = await PositionAssignmentService.GetActiveAsync(p.Id, _cts.Token);
            History = [.. (await PositionAssignmentService.GetHistoryAsync(p.Id, limit: 25, _cts.Token))];
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }

        await InvokeAsync(StateHasChanged);
    }

    private void OpenAssignModal()
    {
        if (SelectedPerson is null) return;
        _assignModal?.Open(SelectedPerson);
    }

    private void OpenUnassignModal()
    {
        if (SelectedPerson is null || ActiveAssign is null) return;
        _unassignModal?.Open(SelectedPerson, ActiveAssign);
    }

    private async Task HandleAssigned(PersonPositionAssignment created)
    {
        Toast.ShowSuccess("Призначення виконано.");

        // оновлюємо дані по вибраній особі
        if (SelectedPerson is null) return;

        ActiveAssign = created;
        SelectedPerson.PositionUnitId = created.PositionUnitId;
        SelectedPerson.PositionUnit = created.PositionUnit;

        // оновити історію (додати новий на початок)
        History.Insert(0, created);

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleUnassigned(bool ok)
    {
        if (!ok) return;

        Toast.ShowSuccess("Зняття з посади виконано.");

        if (SelectedPerson is null) return;

        // перевантажимо актив і історію
        ActiveAssign = await PositionAssignmentService.GetActiveAsync(SelectedPerson.Id, _cts.Token);
        History = [.. (await PositionAssignmentService.GetHistoryAsync(SelectedPerson.Id, 25, _cts.Token))];

        // у person зняти покажчик
        SelectedPerson.PositionUnitId = null;
        SelectedPerson.PositionUnit = null;

        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}