//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CloseCombatTaskDrawer
//-----------------------------------------------------------------------------

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

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для завершення участі в місії (часткове закриття: місія + особи).
/// Flow:
/// 1) SourceDoc + дата закриття
/// 2) вибір місії
/// 3) підтягуємо активних осіб на місії на дату (To == null, From <= date)
/// 4) локальний пошук + вибір осіб
/// 5) Save -> End-деталі
/// </summary>
public partial class CloseCombatTaskDrawer
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;
    [Inject] public IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>> LookupMissionPersons { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<CreateCombatTaskModel> OnCreated { get; set; }

    //======================================================================
    // UI state: meta
    //======================================================================

    private bool _busy;
    private CreateCombatTaskModel _model = new();
    private DateOnly _at = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // UI state: missions
    //======================================================================

    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];
    private string _missionDisplay = string.Empty;

    //======================================================================
    // UI state: persons (prefetch per mission/date -> local filter)
    //======================================================================

    private const int MinSearchLength = 2;

    private bool _personsLoading;
    private string _personSearch = string.Empty;

    private IReadOnlyList<ActiveMissionPersonDto> _personsAll = [];
    private IReadOnlyList<ActiveMissionPersonDto> _personResults = [];

    private readonly Dictionary<Guid, ActiveMissionPersonDto> _selected = [];

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        Reset();
        await EnsureMissionsLoadedAsync();

        // persons не вантажимо, доки місію не обрали
    }

    private void Reset()
    {
        _busy = false;

        _model = new CreateCombatTaskModel
        {
            SourceDocument = string.Empty,
            MissionId = Guid.Empty,
            Details = []
        };

        _at = DateOnly.FromDateTime(DateTime.Now);

        _missionDisplay = string.Empty;

        _personsLoading = false;
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

    private async Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return;

        _model.MissionId = id;

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _model.MissionId);
        _missionDisplay = m is null ? string.Empty : BuildMissionDisplayUa(m);

        await LoadMissionPersonsAsync();
    }

    private void ClearMission()
    {
        _model.MissionId = Guid.Empty;
        _missionDisplay = string.Empty;

        _personsAll = [];
        _personResults = [];
        _selected.Clear();
    }

    //======================================================================
    // Date changes
    //======================================================================

    private async Task OnAtChangedAsync()
    {
        // якщо місію не обрано — нема що вантажити
        if (_model.MissionId == Guid.Empty) return;

        await LoadMissionPersonsAsync();
    }

    //======================================================================
    // Persons: load + local filter
    //======================================================================

    private async Task LoadMissionPersonsAsync()
    {
        if (_model.MissionId == Guid.Empty) return;

        _personsLoading = true;
        try
        {
            var res = await LookupMissionPersons.HandleAsync(
                new GetCombatTaskMissionPersonsQuery(_model.MissionId, _at));

            _personsAll = [.. res
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Rnokpp)
                .ThenBy(x => x.PersonId)];

            // якщо selected стала неактуальна — прибираємо
            var allowed = _personsAll.Select(x => x.PersonId).ToHashSet();
            foreach (var pid in _selected.Keys.ToList())
                if (!allowed.Contains(pid))
                    _selected.Remove(pid);

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
        var s = (_personSearch ?? string.Empty).Trim();

        if (s.Length < MinSearchLength)
        {
            _personResults = _personsAll;
            return;
        }

        _personResults = [.. _personsAll.Where(p =>
            ContainsIgnoreCase(p.FullName, s) ||
            ContainsIgnoreCase(p.Rnokpp, s) ||
            (!string.IsNullOrWhiteSpace(p.Callsign) && ContainsIgnoreCase(p.Callsign!, s)))];
    }

    //======================================================================
    // Save / Close
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

            _model.Details = [.. _selected.Values.Select(p => new CreateCombatTaskDetailsModel
            {
                PersonId = p.PersonId,
                CombatTaskDetailsKind = CombatTaskDetailsKind.End,
                EffectiveAt = _at,
                Rnokpp = p.Rnokpp,
                FullName = p.FullName,
                Callsign = p.Callsign
            })];

            await OnCreated.InvokeAsync(_model);
            await Close();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private Task Close()
        => IsOpenChanged.InvokeAsync(false);

    //======================================================================
    // View helpers
    //======================================================================

    private string PersonsEmptyHint
    {
        get
        {
            if (_model.MissionId == Guid.Empty) return "Оберіть місію — підтягнемо осіб, які на ній знаходяться.";
            if (_personsLoading) return "Завантаження...";
            if (_personsAll.Count == 0) return "На цю дату на місії немає активних осіб.";
            if ((_personSearch?.Trim().Length ?? 0) >= MinSearchLength && _personResults.Count == 0)
                return "Нічого не знайдено за пошуком.";
            return $"Введіть мін. {MinSearchLength} символи для пошуку або обирайте зі списку.";
        }
    }

    private static bool ContainsIgnoreCase(string value, string search)
        => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    //======================================================================
    // Mission grouping/formatting (same as Start drawer)
    //======================================================================

    private IReadOnlyList<MissionGroupVm> MissionGroups
        => [.. _missionOptions
            .GroupBy(m => m.MissionMode)
            .OrderBy(g => (int)g.Key)
            .Select(g => new MissionGroupVm(g.Key, [.. g]))];

    private sealed record MissionGroupVm(MissionMode Mode, IReadOnlyList<MissionDto> Items)
    {
        public int Count => Items.Count;
    }

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

    private ConfirmModal<ActiveMissionPersonDto>? _personConfirm;

    private async Task ToggleWithConfirmAsync(ActiveMissionPersonDto p)
    {
        if (_personConfirm is null) return;

        var willSelect = !_selected.ContainsKey(p.PersonId);

        var ok = await _personConfirm.ShowAsync(
            p,
            bodyText: willSelect
                ? "Додати особу до списку?"
                : "Прибрати особу зі списку?");

        if (!ok) return;

        if (willSelect) _selected[p.PersonId] = p;
        else _selected.Remove(p.PersonId);
    }

    private string GetToggleBtnClass(ActiveMissionPersonDto r)
       => _selected.ContainsKey(r.PersonId)
           ? "fw-bold btn-sm btn-success rounded-0"   // вибраний -> мінус (прибрати)
           : "fw-bold btn-sm btn-light rounded-0"; // не вибраний -> плюс (додати)
}
