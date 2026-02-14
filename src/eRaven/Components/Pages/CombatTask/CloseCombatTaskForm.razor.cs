//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseCombatTaskForm
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

public partial class CloseCombatTaskForm : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;
    [Inject] public IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>> LookupMissionPersons { get; set; } = default!;
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
    private DateOnly _at = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // Missions
    //======================================================================

    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];
    private string _missionDisplay = string.Empty;

    //======================================================================
    // Persons (loaded by mission/date -> local filter)
    //======================================================================

    private const int MinSearchLength = 2;

    private bool _personsLoading;
    private string _personSearch = string.Empty;

    private IReadOnlyList<ActiveMissionPersonDto> _personsAll = [];
    private IReadOnlyList<ActiveMissionPersonDto> _personResults = [];

    private readonly Dictionary<Guid, ActiveMissionPersonDto> _selected = [];

    // confirm modal (on add)
    private ConfirmModal<ActiveMissionPersonDto>? _personConfirm;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        if (DocumentId == Guid.Empty) return;

        ResetForClose();
        await EnsureMissionsLoadedAsync();

        // persons не вантажимо доки не оберуть місію
    }

    private void ResetForClose()
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

    private IReadOnlyList<MissionGroupModel> MissionGroups
        => [.. _missionOptions
            .GroupBy(m => m.MissionMode)
            .OrderBy(g => (int)g.Key)
            .Select(g => new MissionGroupModel(g.Key, [.. g]))];

    private sealed record MissionGroupModel(MissionMode Mode, IReadOnlyList<MissionDto> Items)
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

    private async Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return;

        _model.MissionId = id;

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _model.MissionId);
        _missionDisplay = m is null ? string.Empty : BuildMissionDisplayUa(m);

        // при зміні місії — очищаємо поточний вибір
        _selected.Clear();

        await LoadMissionPersonsAsync();
    }

    //======================================================================
    // Date change
    //======================================================================

    private async Task OnAtChangedAsync()
    {
        if (_model.MissionId == Guid.Empty) return;

        // на іншу дату склад місії може змінитись
        _selected.Clear();
        await LoadMissionPersonsAsync();
    }

    //======================================================================
    // Persons: load + filter
    //======================================================================

    private async Task LoadMissionPersonsAsync()
    {
        if (_model.MissionId == Guid.Empty) return;

        _personsLoading = true;
        try
        {
            var res = await LookupMissionPersons.HandleAsync(
                new GetCombatTaskMissionPersonsQuery(_model.MissionId, IsPlanned: true, _at));

            _personsAll = [.. res
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.Rnokpp)
                .ThenBy(x => x.PersonId)];

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

        if (_model.MissionId == Guid.Empty)
        {
            _personResults = [];
            return;
        }

        if (s.Length < MinSearchLength)
        {
            _personResults = _personsAll;
            return;
        }

        _personResults = [.. _personsAll.Where(p =>
            ContainsIgnoreCase(p.FullName, s) ||
            ContainsIgnoreCase(p.Rnokpp, s) ||
            ContainsIgnoreCase(p.Callsign, s) ||
            ContainsIgnoreCase(p.Rank, s) ||
            ContainsIgnoreCase(p.Position, s) ||
            ContainsIgnoreCase(p.Weapon, s))];
    }

    private static bool ContainsIgnoreCase(string? value, string search)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains(search, StringComparison.OrdinalIgnoreCase);

    //======================================================================
    // Actions: add/remove with confirm on add
    //======================================================================

    private async Task OnPersonActionAsync(ActiveMissionPersonDto p)
    {
        if (_busy) return;

        var selected = _selected.ContainsKey(p.PersonId);

        if (selected)
        {
            _selected.Remove(p.PersonId);
            return;
        }

        if (_personConfirm is null) return;

        var ok = await _personConfirm.ShowAsync(
            p,
            bodyText: "Додати особу до списку закриття участі в завдання?");

        if (!ok) return;

        _selected[p.PersonId] = p;
    }

    private void RemoveSelected(Guid personId)
    {
        if (_busy) return;
        _selected.Remove(personId);
    }

    private void ClearSelected()
    {
        if (_busy) return;
        _selected.Clear();
    }

    //======================================================================
    // UI helpers
    //======================================================================

    private string GetActionBtnClass(ActiveMissionPersonDto p)
        => _selected.ContainsKey(p.PersonId)
             ? "fw-bold btn-sm btn-success rounded-0"   // вибраний -> мінус (прибрати)
             : "fw-bold btn-sm btn-light rounded-0";    // не вибраний -> плюс (додати)

    private string GetActionLabel(ActiveMissionPersonDto p)
        => _selected.ContainsKey(p.PersonId) ? "−" : "+";

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

            _model.Details = [.. _selected.Values.Select(p => new CreateCombatTaskDetailsModel
            {
                PersonId = p.PersonId,
                CombatTaskDetailsKind = CombatTaskDetailsKind.End,
                EffectiveAt = _at,

                // snapshot light
                Rnokpp = p.Rnokpp,
                FullName = p.FullName,
                Callsign = p.Callsign
            })];

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
                    Callsign: x.Callsign
                ))]);

            await CreateCombatTaskHandler.HandleAsync(command);

            Toasts.Success($"Повернення з завдання '{_model.SourceDocument}' успішно виконано.");
            Nav.NavigateTo($"/task-document/{DocumentId}");
        }
        catch (Exception ex)
        {
            Toasts.Error($"Помилка при поверненні з завдання: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    private void GoBack()
        => Nav.NavigateTo($"/task-document/{DocumentId}");
}
