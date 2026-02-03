//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupCreateDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Application.Queries.Mission;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для створення "групи" (CombatTaskEntry group) у документі.
/// Створення включає:
/// - метадані (джерело / дія / дата)
/// - вибір місії (з group select + scroll)
/// - вибір осіб (lookup + список)
/// </summary>
public partial class CombatGroupCreateDrawer
{
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;
    [Inject] public IQueryHandler<SearchFreePersonsForCombatTaskQuery, IReadOnlyList<CombatTaskPersonLookupDto>> SearchFreePersons { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Callback при створенні групи.</summary>
    [Parameter] public EventCallback<StartCombatTaskGroupModel> OnCreated { get; set; }

    private string _sourceDocNo = string.Empty;
    private DateOnly _from = DateOnly.FromDateTime(DateTime.Now);

    // missions state (як було)
    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];
    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    private const int PersonTake = 80;
    private string _personSearch = string.Empty;
    private IReadOnlyList<CombatTaskPersonLookupDto> _personResults = [];
    private readonly Dictionary<Guid, CombatTaskPersonLookupDto> _selected = [];
    private int _personSearchVersion;

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        await EnsureMissionsLoadedAsync();
        ResetForCreate();
        _personResults = [];
    }

    private void ResetForCreate()
    {
        _sourceDocNo = string.Empty;
        _from = DateOnly.FromDateTime(DateTime.Now);
        ClearMission();

        _personSearch = string.Empty;
        _selected.Clear();
    }

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

    private Task OnFromChangedAsync()
    {
        // якщо міняємо дату — "вільні" змінюються: очищаємо вибір та результати
        _selected.Clear();
        _personResults = [];
        return Task.CompletedTask;
    }

    private async Task OnPersonSearchAfterBindingAsync()
    {
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

        _personResults = await SearchFreePersons.HandleAsync(new SearchFreePersonsForCombatTaskQuery(
            Date: _from,
            Search: s,
            Take: PersonTake));
    }

    private void TogglePerson(CombatTaskPersonLookupDto dto, ChangeEventArgs e)
    {
        var value = e.Value is bool b && b;

        if (value) _selected[dto.PersonId] = dto;
        else _selected.Remove(dto.PersonId);
    }

    private bool CanSave
        => !string.IsNullOrWhiteSpace(_sourceDocNo)
           && _missionId != Guid.Empty
           && _selected.Count > 0;

    private async Task Save()
    {
        try
        {
            var missionDisplay = string.IsNullOrWhiteSpace(_missionDisplay) ? "—" : _missionDisplay;

            await OnCreated.InvokeAsync(new StartCombatTaskGroupModel(
                SourceDocNo: _sourceDocNo.Trim(),
                MissionId: _missionId,
                MissionDisplaySnapshot: missionDisplay,
                From: _from,
                Persons: [.. _selected.Values]));

            Toasts.Success("Групу підготовлено.");
            await Close();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
    }

    private Task Close()
        => IsOpenChanged.InvokeAsync(false);

    private void ClearMission()
    {
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;
    }

    // OnMissionChanged + BuildMissionDisplayUa + MissionGroups — залишай як у тебе (без змін)
}