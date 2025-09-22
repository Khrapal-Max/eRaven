//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentsPage (code-behind)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels.PositionAssignmentViewModels;
using eRaven.Components.Pages.PositionAssignments.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments;

public partial class PositionAssignmentsPage : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private List<Person> _persons = [];
    private List<Person> _filtered = [];

    private List<PositionUnit> _positions = [];
    private List<PositionUnit> _freePositions = [];

    // Експортні рядки — завжди синхронізуємо зі _filtered
    private List<PersonAssignmentExportRow> _exportItems = [];

    private AssignPositionModal? _assignModal;
    private UnassignPositionModal? _unassignModal;

    protected string? Search { get; set; }
    protected bool Busy { get; private set; }

    protected Person? SelectedPerson { get; set; }
    protected PersonPositionAssignment? ActiveAssign { get; set; }
    protected List<PersonPositionAssignment> History { get; private set; } = [];

    [Inject] public IPersonService PersonService { get; set; } = default!;
    [Inject] public IPositionService PositionService { get; set; } = default!;
    [Inject] public IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadPersonsAsync();
        await LoadPositionsAsync();
        RecomputeFreePositions();
        RebuildExportItems();
    }

    // --------------------- Data loading ---------------------

    private async Task LoadPersonsAsync()
    {
        try
        {
            _persons = [.. await PersonService.SearchAsync(null, _cts.Token)];
            await ApplyFilter();            // оновить _filtered
            RecomputeFreePositions();       // важливо після _persons
            RebuildExportItems();           // синхронізуємо експорт
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
            _positions = [.. await PositionService.GetPositionsAsync(onlyActive: true, _cts.Token)];
            RecomputeFreePositions();
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    /// <summary>
    /// Вільні посади: активні та не зайняті жодною особою (по локальній моделі _persons).
    /// </summary>
    private void RecomputeFreePositions()
    {
        var occupied = _persons
            .Where(p => p.PositionUnitId.HasValue)
            .Select(p => p.PositionUnitId!.Value)
            .ToHashSet();

        _freePositions = [.. _positions
            .Where(p => p.IsActived && !occupied.Contains(p.Id))
            .OrderBy(p => p.FullName, StringComparer.Ordinal)];
        StateHasChanged();
    }

    /// <summary>
    /// Побудувати дані для експорту (по поточному фільтру).
    /// </summary>
    private void RebuildExportItems()
    {
        _exportItems = [.. _filtered
            .Select(p => new PersonAssignmentExportRow
            {
                Code = p.PositionUnit?.Code,
                SpecialNumber = p.PositionUnit?.SpecialNumber,
                Position = p.PositionUnit?.FullName ?? "Вакантна",
                Rank = p.Rank,
                FullName = p.FullName,
                Rnokpp = p.Rnokpp
            })];
    }

    // --------------------- UI filtering ---------------------

    private async Task ApplyFilter()
    {
        SelectedPerson = null;
        ActiveAssign = null;
        History.Clear();

        if (string.IsNullOrWhiteSpace(Search))
        {
            _filtered = [.. _persons];
        }
        else
        {
            _filtered = [.. _persons.Where(p =>
                   (p.FullName?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Rnokpp?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Callsign?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false))];
        }

        RebuildExportItems();
        await InvokeAsync(StateHasChanged);
    }

    protected Task OnSearchAsync() => ApplyFilter();

    // --------------------- Events ---------------------

    protected async Task OnPersonClick(Person p)
    {
        SelectedPerson = p;

        try
        {
            ActiveAssign = await PositionAssignmentService.GetActiveAsync(p.Id, _cts.Token);
            History = [.. (await PositionAssignmentService.GetHistoryAsync(p.Id, limit: 25, _cts.Token))];
            RecomputeFreePositions(); // під оновлену selection
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

        var lastClose = History?
            .Where(h => h.CloseUtc.HasValue)
            .OrderByDescending(h => h.CloseUtc!.Value)
            .Select(h => (DateTime?)h.CloseUtc!.Value)
            .FirstOrDefault();

        _assignModal?.Open(SelectedPerson, lastClose);
    }

    private void OpenUnassignModal()
    {
        if (SelectedPerson is null || ActiveAssign is null) return;
        _unassignModal?.Open(SelectedPerson, ActiveAssign);
    }

    // --------------------- Callbacks from modals ---------------------

    private async Task HandleAssigned(PersonPositionAssignment created)
    {
        Toast.ShowSuccess("Призначення виконано.");

        if (SelectedPerson is null) return;

        // Оновити вибраного
        ActiveAssign = created;
        SelectedPerson.PositionUnitId = created.PositionUnitId;
        SelectedPerson.PositionUnit = created.PositionUnit;

        // Синхронізувати у загальному списку
        var idx = _persons.FindIndex(x => x.Id == SelectedPerson.Id);
        if (idx >= 0)
        {
            _persons[idx].PositionUnitId = created.PositionUnitId;
            _persons[idx].PositionUnit = created.PositionUnit;
        }

        // Історія — новий зверху
        History.Insert(0, created);

        RecomputeFreePositions();
        RebuildExportItems();
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleUnassigned(bool ok)
    {
        if (!ok) return;

        Toast.ShowSuccess("Зняття з посади виконано.");

        if (SelectedPerson is null) return;

        ActiveAssign = await PositionAssignmentService.GetActiveAsync(SelectedPerson.Id, _cts.Token);
        History = [.. (await PositionAssignmentService.GetHistoryAsync(SelectedPerson.Id, 25, _cts.Token))];

        // Локально зняти посаду
        SelectedPerson.PositionUnitId = null;
        SelectedPerson.PositionUnit = null;

        var idx = _persons.FindIndex(x => x.Id == SelectedPerson.Id);
        if (idx >= 0)
        {
            _persons[idx].PositionUnitId = null;
            _persons[idx].PositionUnit = null;
        }

        RecomputeFreePositions();
        RebuildExportItems();
        await InvokeAsync(StateHasChanged);
    }

    // --------------------- Busy sync (Excel export) ---------------------

    protected Task OnBusyChanged(bool busy)
    {
        Busy = busy;
        StateHasChanged();
        return Task.CompletedTask;
    }

    // --------------------- Helpers ---------------------

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}