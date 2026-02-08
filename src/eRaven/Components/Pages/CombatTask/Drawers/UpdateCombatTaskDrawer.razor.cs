/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CombatMissionEndDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для завершення участі в місії (часткове закриття: обираємо місію + осіб).
/// UI-flow:
/// 1) дата + документ закриття
/// 2) вибір місії з активних на дату
/// 3) вибір осіб у місії на дату
/// </summary>
public partial class CombatGroupEndDrawer
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>Повертає активні місії на дату (для стадії вибору місії).</summary>
    [Inject] public IQueryHandler<GetActiveMissionsOnDateQuery, IReadOnlyList<ActiveMissionOptionDto>> GetActiveMissions { get; set; } = default!;

    /// <summary>Повертає активних осіб у місії на дату (для стадії вибору осіб).</summary>
    [Inject] public IQueryHandler<GetActivePersonsForMissionOnDateQuery, IReadOnlyList<CombatTaskPersonLookupDto>> GetActivePersons { get; set; } = default!;

    /// <summary>Toast повідомлення.</summary>
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Callback у DocumentEditor після підтвердження.</summary>
    [Parameter] public EventCallback<EndCombatMissionModel> OnEnded { get; set; }

    //======================================================================
    // State: meta
    //======================================================================

    private DateOnly _to = DateOnly.FromDateTime(DateTime.Now);
    private string _endSourceDocNo = string.Empty;

    //======================================================================
    // State: stage
    //======================================================================

    private bool _missionSelected;
    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    //======================================================================
    // State: missions
    //======================================================================

    private bool _missionsLoading;
    private string _missionSearch = string.Empty;
    private IReadOnlyList<ActiveMissionOptionDto> _missions = [];
    private int _missionSearchVersion;

    //======================================================================
    // State: persons
    //======================================================================

    private bool _personsLoading;
    private string _personSearch = string.Empty;
    private IReadOnlyList<CombatTaskPersonLookupDto> _persons = [];
    private readonly Dictionary<Guid, CombatTaskPersonLookupDto> _selected = [];
    private int _personSearchVersion;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        ResetForOpen();
        await LoadMissionsAsync();
    }

    private void ResetForOpen()
    {
        _to = DateOnly.FromDateTime(DateTime.Now);
        _endSourceDocNo = string.Empty;

        _missionSelected = false;
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;

        _missionSearch = string.Empty;
        _missions = [];

        _personSearch = string.Empty;
        _persons = [];
        _selected.Clear();
    }

    //======================================================================
    // Meta changes
    //======================================================================

    private async Task OnToChangedAsync()
    {
        // Зміна дати => набір активних місій/осіб змінюється.
        _missionSelected = false;
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;

        _persons = [];
        _selected.Clear();

        await LoadMissionsAsync();
    }

    //======================================================================
    // Stage: missions
    //======================================================================

    private async Task LoadMissionsAsync()
    {
        _missionsLoading = true;
        try
        {
            var s = (_missionSearch ?? string.Empty).Trim();
            if (s.Length < 2) s = string.Empty;

            _missions = await GetActiveMissions.HandleAsync(new GetActiveMissionsOnDateQuery(
                Date: _to,
                Search: string.IsNullOrWhiteSpace(s) ? null : s));
        }
        finally
        {
            _missionsLoading = false;
        }
    }

    private void SelectMission(ActiveMissionOptionDto m)
    {
        _missionId = m.MissionId;
        _missionDisplay = m.MissionDisplaySnapshot ?? string.Empty;
    }

    private async Task OnMissionSearchAfterBindingAsync()
    {
        var version = ++_missionSearchVersion;
        await Task.Delay(150);

        if (version != _missionSearchVersion)
            return;

        await LoadMissionsAsync();
    }

    private async Task GoToPersonsStage()
    {
        if (_missionId == Guid.Empty) return;

        _missionSelected = true;
        _personSearch = string.Empty;
        _selected.Clear();

        await LoadPersonsAsync(search: null);
    }

    private Task BackToMissionsStage()
    {
        _missionSelected = false;
        _persons = [];
        _selected.Clear();
        return Task.CompletedTask;
    }

    //======================================================================
    // Stage: persons
    //======================================================================

    private async Task LoadPersonsAsync(string? search)
    {
        _personsLoading = true;
        try
        {
            _persons = await GetActivePersons.HandleAsync(new GetActivePersonsForMissionOnDateQuery(
                Date: _to,
                MissionId: _missionId,
                Search: string.IsNullOrWhiteSpace(search) ? null : search.Trim()));
        }
        finally
        {
            _personsLoading = false;
        }
    }

    private async Task OnPersonSearchAfterBindingAsync()
    {
        var version = ++_personSearchVersion;
        await Task.Delay(150);

        if (version != _personSearchVersion)
            return;

        var s = (_personSearch ?? string.Empty).Trim();

        // Як у create drawer: пошук мінімум 2 символи, інакше показуємо "всі"
        if (s.Length < 2)
        {
            await LoadPersonsAsync(search: null);
            return;
        }

        await LoadPersonsAsync(search: s);
    }

    private void TogglePerson(CombatTaskPersonLookupDto dto, ChangeEventArgs e)
    {
        var value = e.Value is bool b && b;
        if (value) _selected[dto.PersonId] = dto;
        else _selected.Remove(dto.PersonId);
    }

    //======================================================================
    // Save / Close
    //======================================================================

    private bool CanSave
        => _missionSelected
           && _missionId != Guid.Empty
           && _selected.Count > 0
           && !string.IsNullOrWhiteSpace(_endSourceDocNo);

    private async Task Save()
    {
        try
        {
            if (_missionId == Guid.Empty)
                throw new InvalidOperationException("Не обрано місію.");

            if (_selected.Count == 0)
                throw new InvalidOperationException("Не обрано осіб.");

            if (string.IsNullOrWhiteSpace(_endSourceDocNo))
                throw new InvalidOperationException("Вкажіть № документа закриття.");

            await OnEnded.InvokeAsync(new EndCombatMissionModel(
                MissionId: _missionId,
                PersonIds: [.. _selected.Keys],
                To: _to,
                EndSourceDocNo: _endSourceDocNo.Trim()));

            Toasts.Success("Закриття підготовлено.");
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
*/