//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.CombatTask.Models;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Application.Queries.Mission;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для створення CombatTask (групи участей) у межах документа.
/// Підтягує "вільних на дату" один раз і фільтрує локально по пошуку.
/// </summary>
public partial class CreateCombatTaskDrawer
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;
    [Inject] public IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>> LookupPersons { get; set; } = default!;
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
    private DateOnly _from = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // UI state: missions
    //======================================================================

    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];
    private string _missionDisplay = string.Empty;

    //======================================================================
    // UI state: persons (prefetch once -> local filter)
    //======================================================================

    private const int MinSearchLength = 2;

    private bool _personsLoading;
    private string _personSearch = string.Empty;

    private IReadOnlyList<ReadyCombatTaskPersonDto> _personsAll = [];
    private IReadOnlyList<ReadyCombatTaskPersonDto> _personResults = [];

    private readonly Dictionary<Guid, ReadyCombatTaskPersonDto> _selected = [];

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        await EnsureMissionsLoadedAsync();

        ResetForCreate();
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

    private IReadOnlyList<MissionGroupVm> MissionGroups
        => [.. _missionOptions
            .GroupBy(m => m.MissionMode)
            .OrderBy(g => (int)g.Key)
            .Select(g => new MissionGroupVm(g.Key, [.. g ]))];

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

    private Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return Task.CompletedTask;

        _model.MissionId = id;

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _model.MissionId);
        _missionDisplay = m is null ? string.Empty : BuildMissionDisplayUa(m);

        return Task.CompletedTask;
    }

    private void ClearMission()
    {
        _model.MissionId = Guid.Empty;
        _missionDisplay = string.Empty;
    }

    //======================================================================
    // Persons: prefetch + local filter
    //======================================================================

    private async Task OnFromChangedAsync()
        => await LoadFreePersonsAsync(_from);

    private async Task LoadFreePersonsAsync(DateOnly onDate)
    {
        _personsLoading = true;
        try
        {
            // 1) префетч "вільних" на дату
            var res = await LookupPersons.HandleAsync(new GetCombatTaskPersonLookupQuery(onDate));

            // 2) детермінований порядок
            _personsAll = [.. res
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Rnokpp)
                .ThenBy(x => x.PersonId)];

            // 3) якщо хтось вже selected, але зник зі списку "вільних" — прибрати
            var allowed = _personsAll.Select(x => x.PersonId).ToHashSet();
            foreach (var id in _selected.Keys.ToList())
            {
                if (!allowed.Contains(id))
                    _selected.Remove(id);
            }

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

        _personResults = [.. _personsAll
            .Where(p =>
                ContainsIgnoreCase(p.FullName, s) ||
                ContainsIgnoreCase(p.Rnokpp, s) ||
                (!string.IsNullOrWhiteSpace(p.Callsign) && ContainsIgnoreCase(p.Callsign!, s)))];
    }

    private static bool ContainsIgnoreCase(string value, string search)
        => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private void TogglePerson(ReadyCombatTaskPersonDto r, ChangeEventArgs e)
    {
        var isChecked = e.Value switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("on", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        if (isChecked)
            _selected[r.PersonId] = r;
        else
            _selected.Remove(r.PersonId);
    }

    private string PersonsEmptyHint
    {
        get
        {
            if (_personsLoading) return "Завантаження...";
            if (_personsAll.Count == 0) return "Немає вільних осіб на обрану дату.";
            if ((_personSearch?.Trim().Length ?? 0) >= MinSearchLength && _personResults.Count == 0)
                return "Нічого не знайдено за пошуком.";
            return $"Введіть мін. {MinSearchLength} символи для пошуку або обирайте зі списку.";
        }
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
                CombatTaskDetailsKind = CombatTaskDetailsKind.Start,
                EffectiveAt = _from,
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
}
