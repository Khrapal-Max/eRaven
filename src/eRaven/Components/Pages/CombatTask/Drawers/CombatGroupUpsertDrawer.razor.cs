//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupUpsertDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Application.Queries.Mission;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для створення або редагування "групи" (CombatTaskEntry group) у документі.
/// - Mode=create: вибір місії + заповнення метаданих + вибір осіб (snapshot піде в entries).
/// - Mode=edit: редагування тільки метаданих групи (осіб змінює окремий drawer).
/// </summary>
public partial class CombatGroupUpsertDrawer
{
    //========================
    // DI
    //========================

    /// <summary>Повертає список доступних місій (наприклад, тільки активні).</summary>
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> GetMissions { get; set; } = default!;

    /// <summary>Швидкий пошук осіб для picker-а (без paging на 500).</summary>
    [Inject] public IQueryHandler<SearchPersonsForCombatTaskQuery, IReadOnlyList<CombatTaskPersonLookupDto>> SearchPersons { get; set; } = default!;

    [Inject] public ToastService Toasts { get; set; } = default!;

    //========================
    // Parameters
    //========================

    /// <summary>"create" або "edit".</summary>
    [Parameter] public string Mode { get; set; } = "create";

    /// <summary>GroupId для edit режиму.</summary>
    [Parameter] public Guid GroupId { get; set; }

    /// <summary>Початкові дані для edit режиму.</summary>
    [Parameter] public CreateCombatGroupModel? Initial { get; set; }

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public EventCallback<CreateCombatGroupModel> OnCreated { get; set; }
    [Parameter] public EventCallback<UpdateCombatGroupModel> OnUpdated { get; set; }

    //========================
    // State
    //========================
    private string _sourceDocNo = string.Empty;
    private ActionKind _action = ActionKind.Start;
    private DateOnly _actionDate = DateOnly.FromDateTime(DateTime.Now);

    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    private string _missionSearch = string.Empty;
    private IReadOnlyList<MissionDto> _missions = [];
    private IReadOnlyList<MissionDto> _missionsFiltered = [];

    private string _personSearch = string.Empty;
    private IReadOnlyList<CombatTaskPersonLookupDto> _personResults = [];
    private HashSet<Guid> _selected = [];

    // simple debounce versioning
    private int _personSearchVersion;

    //========================
    // Lifecycle
    //========================
    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        await EnsureMissionsLoadedAsync();

        if (Mode == "edit" && Initial is not null)
        {
            ApplyInitialForEdit();
        }
        else
        {
            ResetForCreate();
        }

        FilterMissions();

        // Persons picker only on create
        if (Mode == "create")
        {
            _personResults = [];
        }
    }

    private async Task EnsureMissionsLoadedAsync()
    {
        if (_missions.Count != 0) return;

        _missions = await GetMissions.HandleAsync(new GetMissionsQuery(true, null, null));
        _missionsFiltered = _missions;
    }

    private void ApplyInitialForEdit()
    {
        _sourceDocNo = Initial!.SourceDocNo;
        _action = Initial.Action;
        _actionDate = Initial.ActionDate;
        _missionId = Initial.MissionId;
        _missionDisplay = Initial.MissionDisplaySnapshot;
    }

    private void ResetForCreate()
    {
        _sourceDocNo = string.Empty;
        _action = ActionKind.Start;
        _actionDate = DateOnly.FromDateTime(DateTime.Now);
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;

        _missionSearch = string.Empty;
        _personSearch = string.Empty;
        _selected = [];
    }

    //========================
    // Mission picker
    //========================
    private void FilterMissions()
    {
        var s = (_missionSearch ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(s))
        {
            _missionsFiltered = _missions;
            return;
        }

        _missionsFiltered = [.. _missions.Where(m => (m.DisplayMisssion ?? string.Empty).Contains(s, StringComparison.OrdinalIgnoreCase))];
    }

    private void SelectMission(MissionDto m)
    {
        _missionId = m.MissionId;
        _missionDisplay = m.DisplayMisssion ?? string.Empty;
    }

    //========================
    // Persons search (debounced)
    //========================
    private async Task OnPersonSearchChangedAsync()
    {
        if (Mode != "create") return;

        var version = ++_personSearchVersion;
        await Task.Delay(150);

        if (version != _personSearchVersion)
            return;

        var s = (_personSearch ?? string.Empty).Trim();

        if (s.Length < 2)
        {
            _personResults = [];
            return;
        }

        _personResults = await SearchPersons.HandleAsync(new SearchPersonsForCombatTaskQuery(s, 80));
    }

    private async Task OnPersonSearchInput(ChangeEventArgs _)
    {
        await OnPersonSearchChangedAsync();
    }

    private void OnMissionSearchInput(ChangeEventArgs _)
    {
        FilterMissions();
    }

    private void TogglePerson(Guid id, bool value)
    {
        if (value) _selected.Add(id);
        else _selected.Remove(id);
    }

    //========================
    // Save / Close
    //========================
    private bool CanSave
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_sourceDocNo)) return false;
            if (_missionId == Guid.Empty) return false;

            if (Mode == "create" && _selected.Count == 0) return false;
            return true;
        }
    }

    private async Task Save()
    {
        try
        {
            var missionDisplay = _missions.FirstOrDefault(x => x.MissionId == _missionId)?.DisplayMisssion ?? _missionDisplay;
            if (string.IsNullOrWhiteSpace(missionDisplay))
                missionDisplay = "—";

            if (Mode == "create")
            {
                var model = new CreateCombatGroupModel(
                    SourceDocNo: _sourceDocNo.Trim(),
                    Action: _action,
                    MissionId: _missionId,
                    MissionDisplaySnapshot: missionDisplay,
                    ActionDate: _actionDate,
                    PersonIds: [.. _selected]);

                await OnCreated.InvokeAsync(model);
                Toasts.Success("Завдання додано.");
                await Close();
                return;
            }

            var upd = new UpdateCombatGroupModel(
                GroupId: GroupId,
                SourceDocNo: _sourceDocNo.Trim(),
                Action: _action,
                MissionId: _missionId,
                MissionDisplaySnapshot: missionDisplay,
                ActionDate: _actionDate);

            await OnUpdated.InvokeAsync(upd);
            Toasts.Success("Завдання оновлено.");
            await Close();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
    }

    private Task Close()
        => IsOpenChanged.InvokeAsync(false);
}