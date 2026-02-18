//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StartCombatTaskForm
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.CombatTask.Models;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Application.Queries.Mission;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class StartCombatTaskForm : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>> LookupPersons { get; set; } = default!;
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;
    [Inject] public ICommandHandler<CreateCombatTaskCommand, Guid> CreateCombatTaskHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    //======================================================================
    // Route params
    //======================================================================
    [Parameter] public Guid DocumentId { get; set; }

    //======================================================================
    // UI state: meta
    //======================================================================
    private bool _busy;

    private CreateCombatTaskModel _model = new();
    private DateOnly _from = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // Missions
    //======================================================================
    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];
    private string _missionDisplay = string.Empty;

    //======================================================================
    // Persons
    //======================================================================
    private const int MinSearchLength = 2;

    private bool _personsLoading;
    private bool _showSelectedOnly;
    private string _personSearch = string.Empty;

    private IReadOnlyList<ReadyCombatTaskPersonDto> _personsAll = [];
    private IReadOnlyList<ReadyCombatTaskPersonDto> _personResults = [];

    private readonly Dictionary<Guid, ReadyCombatTaskPersonDto> _selected = [];

    // confirm modal
    private ConfirmModal<ReadyCombatTaskPersonDto>? _personConfirm;

    //======================================================================
    // Lifecycle
    //======================================================================
    protected override async Task OnParametersSetAsync()
    {
        if (DocumentId == Guid.Empty) return;

        ResetForCreate();

        await EnsureMissionsLoadedAsync();
        await LoadFreePersonsAsync(_from);
    }

    private void ResetForCreate()
    {
        _busy = false;

        _model = new CreateCombatTaskModel
        {
            SourceDocument = string.Empty,
            MissionId = Guid.Empty,
            Details = []
        };

        _from = DateOnly.FromDateTime(DateTime.Now);
        _missionDisplay = string.Empty;

        _personsLoading = false;
        _showSelectedOnly = false;
        _personSearch = string.Empty;

        _personsAll = [];
        _personResults = [];
        _selected.Clear();
    }

    //======================================================================
    // Missions
    //======================================================================
    private async Task EnsureMissionsLoadedAsync()
    {
        if (_missionOptions.Count != 0) return;

        _missionsLoading = true;
        try
        {
            _missionOptions = await LookupMissions.HandleAsync(new GetMissionsQuery(true, null, null));
        }
        finally
        {
            _missionsLoading = false;
        }
    }

    private IReadOnlyList<MissionGroupModel> MissionGroups
        => [.. _missionOptions
            .GroupBy(m => m.MissionMode)
            .OrderBy(g => (int)g.Key)
            .Select(g => new MissionGroupModel(g.Key, [.. g]))];

    private static string GetMissionGroupLabel(MissionMode mode, int count)
        => $"{MissionModeLabel(mode)} ({count})";

    private static string MissionModeLabel(MissionMode mode)
        => mode switch
        {
            MissionMode.Day => "День",
            MissionMode.Night => "Ніч",
            MissionMode.FullTime => "Цілодобово",
            _ => mode.ToString()
        };

    private static string BuildMissionDisplayUa(MissionDto m)
    {
        var area = string.IsNullOrWhiteSpace(m.PositionArea) ? "—" : m.PositionArea;
        var point = string.IsNullOrWhiteSpace(m.NamePoint) ? "—" : m.NamePoint;
        var drone = string.IsNullOrWhiteSpace(m.DroneName) ? "—" : m.DroneName;
        var target = string.IsNullOrWhiteSpace(m.Target) ? "—" : m.Target;

        return $"ПЗ: {area} · {point} · ТЗ: {drone} · Режим: {MissionModeLabel(m.MissionMode)} · {target}";
    }

    private static string FormatMissionOption(MissionDto m)
    {
        var area = m.PositionArea;
        var point = m.NamePoint;
        var mode = MissionModeLabel(m.MissionMode);

        var drone = string.IsNullOrWhiteSpace(m.DroneName) ? "—" : TrimTo(m.DroneName!, 20);
        var target = m.Target;

        return $"{area} · {point} · {drone} · ({mode}) · {target}";
    }

    private static string TrimTo(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (value.Length <= max) return value;
        return value[..(max - 1)] + "…";
    }

    private Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return Task.CompletedTask;

        _model.MissionId = id;

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _model.MissionId);
        _missionDisplay = m is null ? string.Empty : BuildMissionDisplayUa(m);

        return Task.CompletedTask;
    }

    //======================================================================
    // Persons (prefetch + filter)
    //======================================================================
    private async Task OnFromChangedAsync()
        => await LoadFreePersonsAsync(_from);

    private async Task LoadFreePersonsAsync(DateOnly onDate)
    {
        _personsLoading = true;
        try
        {
            var res = await LookupPersons.HandleAsync(new GetCombatTaskPersonLookupQuery(onDate));

            _personsAll = [.. res
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Rnokpp)
                .ThenBy(x => x.PersonId)];

            // якщо selected стала неактуальна — прибрати
            var allowed = _personsAll.Select(x => x.PersonId).ToHashSet();
            foreach (var id in _selected.Keys.ToList())
                if (!allowed.Contains(id))
                    _selected.Remove(id);

            ApplyPersonFilter();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
            _personsAll = [];
            _personResults = [];
            _selected.Clear();
        }
        finally
        {
            _personsLoading = false;
        }
    }

    private Task OnPersonSearchAfterBindingAsync()
    {
        ApplyPersonFilter();
        return Task.CompletedTask;
    }

    private void ApplyPersonFilter()
    {
        IEnumerable<ReadyCombatTaskPersonDto> q = _personsAll;

        if (_showSelectedOnly)
            q = q.Where(x => _selected.ContainsKey(x.PersonId));

        var s = (_personSearch ?? string.Empty).Trim();

        if (s.Length >= MinSearchLength)
        {
            q = q.Where(p =>
                ContainsIgnoreCase(p.FullName, s) ||
                ContainsIgnoreCase(p.Rnokpp, s) ||
                ContainsIgnoreCase(p.Callsign, s) ||
                ContainsIgnoreCase(p.Rank, s) ||
                ContainsIgnoreCase(p.Position, s) ||
                ContainsIgnoreCase(p.Weapon, s));
        }

        _personResults = [.. q];
    }

    private static bool ContainsIgnoreCase(string? value, string search)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains(search, StringComparison.OrdinalIgnoreCase);

    //======================================================================
    // Actions: add/remove with confirm on add
    //======================================================================
    private async Task OnPersonActionAsync(ReadyCombatTaskPersonDto p)
    {
        if (_busy) return;

        var selected = _selected.ContainsKey(p.PersonId);

        if (selected)
        {
            _selected.Remove(p.PersonId);
            if (_showSelectedOnly) ApplyPersonFilter();
            return;
        }

        // add -> confirm modal
        if (_personConfirm is null)
            return;

        var ok = await _personConfirm.ShowAsync(
            p,
            bodyText: "Додати особу до списку призначення на завдання?");

        if (!ok) return;

        _selected[p.PersonId] = p;
        if (_showSelectedOnly) ApplyPersonFilter();
    }

    private void RemoveSelected(Guid personId)
    {
        if (_busy) return;

        if (_selected.Remove(personId) && _showSelectedOnly)
            ApplyPersonFilter();
    }

    private void ClearSelected()
    {
        if (_busy) return;

        _selected.Clear();
        if (_showSelectedOnly) ApplyPersonFilter();
    }

    //======================================================================
    // UI helpers
    //======================================================================
    private string GetActionBtnClass(ReadyCombatTaskPersonDto p)
        => _selected.ContainsKey(p.PersonId)
            ? "fw-bold btn-sm btn-success rounded-0"   // вибраний -> мінус (прибрати)
            : "fw-bold btn-sm btn-light rounded-0";    // не вибраний -> плюс (додати)

    private string GetActionLabel(ReadyCombatTaskPersonDto p)
        => _selected.ContainsKey(p.PersonId) ? "−" : "+";

    private string PersonsEmptyHint
    {
        get
        {
            if (_personsLoading) return "Завантаження...";
            if (_showSelectedOnly && _selected.Count == 0) return "Немає вибраних осіб.";
            if (_personsAll.Count == 0) return "Немає вільних осіб на обрану дату.";
            if ((_personSearch?.Trim().Length ?? 0) >= MinSearchLength && _personResults.Count == 0)
                return "Нічого не знайдено за пошуком.";
            return $"Введіть мін. {MinSearchLength} символи для пошуку або обирайте зі списку.";
        }
    }

    private static string OrDash(string? v)
        => string.IsNullOrWhiteSpace(v) ? "—" : v;

    //======================================================================
    // Save / Nav
    //======================================================================
    private bool CanSave
        => !_busy
           && !string.IsNullOrWhiteSpace(_model.SourceDocument)
           && _model.MissionId != Guid.Empty
           && _selected.Count > 0;

    private async Task Save()
    {
        if (!CanSave) return;

        _busy = true;
        try
        {
            _model.SourceDocument = _model.SourceDocument.Trim();

            // 1) Збираємо Details (як і було)
            _model.Details = [.. _selected.Values.Select(p => new CreateCombatTaskDetailsModel
        {
            PersonId = p.PersonId,
            CombatTaskDetailsKind = CombatTaskDetailsKind.Start,
            EffectiveAt = _from,

            // snapshot light
            Rnokpp = p.Rnokpp,
            FullName = p.FullName,
            Callsign = p.Callsign
        })];

            // 2) Команда (як у DocumentEditor)
            var command = new CreateCombatTaskCommand(
                DocumentId: DocumentId,
                MissionId: _model.MissionId,
                SourceDocument: _model.SourceDocument,
                CombatTaskDetails: [.. _model.Details.Select(x => new CombatTaskDetailsDto(
                CombatTaskDetailsId: Guid.NewGuid(),
                Kind: x.CombatTaskDetailsKind,
                EffectiveAt: x.EffectiveAt,
                PersonId: x.PersonId,
                Rnokpp: x.Rnokpp,
                FullName: x.FullName,
                Rank: x.Rank,
                Position: x.Position,
                Weapon: x.Weapon,
                Callsign: x.Callsign))],
                Author: "ui", // TODO aut user
                NowUtc: DateTime.UtcNow);

            await CreateCombatTaskHandler.HandleAsync(command);

            Toasts.Success($"Призначення на завдання '{_model.SourceDocument}' успішно виконано.");

            // 3) Назад в документ
            Nav.NavigateTo($"/task-document/{DocumentId}");
        }
        catch (Exception ex)
        {
            Toasts.Error($"Помилка при призначенні на завдання: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    private void GoBack()
        => Nav.NavigateTo($"/task-document/{DocumentId}");
}
