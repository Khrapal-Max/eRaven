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
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

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

    protected string? Search { get; set; }
    protected bool Busy { get; private set; }

    protected Person? SelectedPerson { get; set; }
    protected PersonPositionAssignment? ActiveAssign { get; set; }
    protected List<PersonPositionAssignment> History { get; private set; } = [];

    [Inject] public IPersonService PersonService { get; set; } = default!;
    [Inject] public IPositionService PositionService { get; set; } = default!;
    [Inject] public IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;
    [Inject] public ILogger<PositionAssignmentsPage> Logger { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadPersonsAsync();
        await LoadPositionsAsync();
        await RecomputeFreePositionsAsync();
        await RebuildExportItemsAsync();
    }

    // --------------------- Data loading ---------------------

    private async Task LoadPersonsAsync()
    {
        try
        {
            _persons = [.. await PersonService.SearchAsync(null, _cts.Token)];
            await ApplyFilterAsync();            // оновить _filtered
            await RecomputeFreePositionsAsync();       // важливо після _persons
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити список людей"))
            {
                throw;
            }
        }
    }

    private async Task LoadPositionsAsync()
    {
        try
        {
            _positions = [.. await PositionService.GetPositionsAsync(onlyActive: true, _cts.Token)];
            await RecomputeFreePositionsAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити посади"))
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Вільні посади: активні та не зайняті жодною особою (по локальній моделі _persons).
    /// </summary>
    private async Task RecomputeFreePositionsAsync()
    {
        var occupied = _persons
            .Where(p => p.PositionUnitId.HasValue)
            .Select(p => p.PositionUnitId!.Value)
            .ToHashSet();

        var next = _positions
            .Where(p => p.IsActived && !occupied.Contains(p.Id))
            .OrderBy(p => p.FullName, StringComparer.Ordinal)];
        if (SamePositions(_freePositions, next))
        {
            return;
        }

        _freePositions = [.. next];
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Дані для експорту (по поточному фільтру).
    /// </summary>
    private async Task<bool> RebuildExportItemsAsync()
    {
        var next = _filtered
            .Select(p => new PersonAssignmentExportRow
            {
                Code = p.PositionUnit?.Code,
                SpecialNumber = p.PositionUnit?.SpecialNumber,
                Position = p.PositionUnit?.FullName ?? "Вакантна",
                Rank = p.Rank,
                FullName = p.FullName,
                Rnokpp = p.Rnokpp
            })]
            .ToList();

        if (SameExportRows(_exportItems, next))
        {
            return false;
        }

        _exportItems = next;
        await InvokeAsync(StateHasChanged);
        return true;
    }

    // --------------------- UI filtering ---------------------

    private async Task ApplyFilterAsync()
    {
        var term = Search?.Trim();
        List<Person> next;

        if (string.IsNullOrWhiteSpace(term))
        {
            next = [.. _persons];
        }
        else
        {
            next = [.. _persons.Where(p =>
                   (p.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Rnokpp?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.Callsign?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
        }

        if (SamePersons(_filtered, next))
        {
            return;
        }

        _filtered = next;
        SelectedPerson = null;
        ActiveAssign = null;
        History.Clear();

        var exportChanged = await RebuildExportItemsAsync();
        if (!exportChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    protected Task OnSearchAsync() => ApplyFilterAsync();

    // --------------------- Events ---------------------

    protected async Task OnPersonClick(Person p)
    {
        SelectedPerson = p;

        try
        {
            ActiveAssign = await PositionAssignmentService.GetActiveAsync(p.Id, _cts.Token);
            History = [.. (await PositionAssignmentService.GetHistoryAsync(p.Id, limit: 25, _cts.Token))];
            await RecomputeFreePositionsAsync(); // оновимо доступні
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити призначення"))
            {
                throw;
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    private void OpenAssignModal()
    {
        if (SelectedPerson is null) return;

        // # НОВА ЛОГІКА
        // Передаємо в модал поточне активне призначення (якщо є) — щоб він
        // автоматично закрив попередню посаду датою (дата призначення - 1 день).
        // Також, якщо в історії є остання CloseUtc — віддамо її як нижню межу.
        var lastClose = History?
            .Where(h => h.CloseUtc.HasValue)
            .OrderByDescending(h => h.CloseUtc!.Value)
            .Select(h => (DateTime?)h.CloseUtc!.Value)
            .FirstOrDefault();

        _assignModal?.Open(SelectedPerson, ActiveAssign, lastClose);
    }

    // --------------------- Callbacks from modal ---------------------

    private async Task HandleAssigned(PersonPositionAssignment created)
    {
        Toast.ShowSuccess("Призначення виконано.");

        if (SelectedPerson is null) return;

        // Якщо була активна посада — локально відмітимо її закриття (модал вже викликав UnassignAsync)
        if (ActiveAssign is not null)
        {
            ActiveAssign.CloseUtc = created.OpenUtc.AddDays(-1);
            // покладемо в історію (внизу/зверху — опційно). Ми додаємо створене згори.
        }

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

        await RecomputeFreePositionsAsync();
        var exportChanged = await RebuildExportItemsAsync();
        if (!exportChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    // --------------------- Busy sync (Excel export) ---------------------

    protected Task OnBusyChanged(bool busy) => SetBusyAsync(busy);

    // --------------------- Helpers ---------------------

    private async Task SetBusyAsync(bool value)
    {
        if (Busy == value)
        {
            return;
        }

        Busy = value;
        await InvokeAsync(StateHasChanged);
    }

    private static bool SamePersons(IReadOnlyList<Person> current, IReadOnlyList<Person> next)
    {
        if (ReferenceEquals(current, next)) return true;
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].Id != next[i].Id)
            {
                return false;
            }
        }

        return true;
    }
    
    private static bool SamePositions(IReadOnlyList<PositionUnit> current, IEnumerable<PositionUnit> next)
    {
        var nextList = next as IList<PositionUnit> ?? next.ToList();
        if (current.Count != nextList.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].Id != nextList[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameExportRows(IReadOnlyList<PersonAssignmentExportRow> current, IReadOnlyList<PersonAssignmentExportRow> next)
    {
        if (ReferenceEquals(current, next)) return true;
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            var a = current[i];
            var b = next[i];

            if (!string.Equals(a.Code, b.Code, StringComparison.Ordinal) ||
                !string.Equals(a.SpecialNumber, b.SpecialNumber, StringComparison.Ordinal) ||
                !string.Equals(a.Position, b.Position, StringComparison.Ordinal) ||
                !string.Equals(a.Rank, b.Rank, StringComparison.Ordinal) ||
                !string.Equals(a.FullName, b.FullName, StringComparison.Ordinal) ||
                !string.Equals(a.Rnokpp, b.Rnokpp, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryHandleKnownException(Exception ex, string message)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return false;
            case System.ComponentModel.DataAnnotations.ValidationException:
            case FluentValidation.ValidationException:
            case InvalidOperationException:
            case ArgumentException:
            case HttpRequestException:
                Toast.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    private bool TryHandleKnownException(Exception ex, string message)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return false;
            case System.ComponentModel.DataAnnotations.ValidationException:
            case FluentValidation.ValidationException:
            case InvalidOperationException:
            case ArgumentException:
            case HttpRequestException:
                Toast.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
