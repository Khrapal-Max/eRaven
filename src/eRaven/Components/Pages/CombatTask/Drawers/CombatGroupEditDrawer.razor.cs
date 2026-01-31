//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupEditDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Mission;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для редагування "групи" (CombatTaskEntry group) у документі.
/// Редагує:
/// - метадані (джерело / дія / дата)
/// - місію (з group select + scroll)
/// Особи редагуються окремим drawer-ом.
/// </summary>
public partial class CombatGroupEditDrawer
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Повертає список доступних місій.
    /// Використовуємо GetMissionsQuery(OnlyOpen=true, Search=null, Mode=null).
    /// </summary>
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;

    /// <summary>Toast повідомлення.</summary>
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>GroupId, який редагуємо.</summary>
    [Parameter] public Guid GroupId { get; set; }

    /// <summary>Початкові дані групи (для заповнення полів).</summary>
    [Parameter] public CreateCombatGroupModel? Initial { get; set; }

    /// <summary>Callback при збереженні змін.</summary>
    [Parameter] public EventCallback<UpdateCombatGroupModel> OnUpdated { get; set; }

    //======================================================================
    // State: meta
    //======================================================================

    private string _sourceDocNo = string.Empty;
    private ActionKind _action = ActionKind.Start;
    private DateOnly _actionDate = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // State: mission picker
    //======================================================================

    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];

    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>
    /// При відкритті drawer-а:
    /// - завантажуємо місії (1 раз)
    /// - застосовуємо Initial (якщо прийшов)
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        await EnsureMissionsLoadedAsync();

        if (Initial is not null)
            ApplyInitial(Initial);
        else
            ResetFallback();
    }

    /// <summary>Застосовує Initial до state.</summary>
    private void ApplyInitial(CreateCombatGroupModel initial)
    {
        _sourceDocNo = initial.SourceDocNo;
        _action = initial.Action;
        _actionDate = initial.ActionDate;

        _missionId = initial.MissionId;
        _missionDisplay = initial.MissionDisplaySnapshot;

        // Якщо місія є у _missionOptions — оновимо display/meta з актуальних даних.
        SyncSelectedMissionFromOptions();
    }

    /// <summary>Fallback, якщо Initial не передали (захист).</summary>
    private void ResetFallback()
    {
        _sourceDocNo = string.Empty;
        _action = ActionKind.Start;
        _actionDate = DateOnly.FromDateTime(DateTime.Now);

        ClearMission();
    }

    //======================================================================
    // Missions: loading + grouping + formatting
    //======================================================================

    /// <summary>Гарантує, що місії завантажені (GetMissionsQuery(true, null, null)).</summary>
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

    /// <summary>Групи місій для <optgroup>.</summary>
    private IReadOnlyList<MissionGroupDto> MissionGroups
        => BuildMissionGroups(_missionOptions);

    /// <summary>Побудова груп місій (по MissionMode).</summary>
    private static List<MissionGroupDto> BuildMissionGroups(IReadOnlyList<MissionDto> items)
    {
        if (items.Count == 0) return [];

        return [.. items
            .GroupBy(x => x.MissionMode)
            .OrderBy(x => x.Key)
            .Select(g => new MissionGroupDto(
                Mode: g.Key,
                Items: [.. g.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PositionArea)]
            ))];
    }

    /// <summary>Лейбл групи (optgroup).</summary>
    private static string GetMissionGroupLabel(MissionMode mode, int count)
        => $"{MissionModeLabel(mode)} ({count})";

    /// <summary>Локалізований підпис MissionMode.</summary>
    private static string MissionModeLabel(MissionMode mode)
        => mode switch
        {
            MissionMode.Day => "День",
            MissionMode.Night => "Ніч",
            MissionMode.FullTime => "Цілодобово",
            _ => mode.ToString()
        };

    /// <summary>Текст option для select (компактно).</summary>
    private static string FormatMissionOption(MissionDto m)
    {
        var date = m.CreatedAt.ToString("dd.MM");
        var mode = MissionModeLabel(m.MissionMode);

        var area = TrimTo(m.PositionArea, 22);
        var point = TrimTo(m.NamePoint ?? "—", 16);
        var drone = string.IsNullOrWhiteSpace(m.DroneName) ? "—" : TrimTo(m.DroneName!, 16);
        var target = TrimTo(m.Target, 26);

        return $"{date} · ({mode}) · {area} · {point} · {drone} · {target}";
    }

    /// <summary>Обрізає строку до max і додає "…".</summary>
    private static string TrimTo(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (value.Length <= max) return value;
        return value[..(max - 1)] + "…";
    }

    //======================================================================
    // Missions: selection
    //======================================================================

    /// <summary>
    /// Обробник зміни місії (onchange).
    /// Гарантовано оновлює _missionDisplay для snapshot.
    /// </summary>
    private Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return Task.CompletedTask;

        _missionId = id;
        SyncSelectedMissionFromOptions();
        return Task.CompletedTask;
    }

    /// <summary>Очищає вибір місії.</summary>
    private void ClearMission()
    {
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;
    }

    /// <summary>
    /// Синхронізує Display/Meta з _missionOptions по поточному _missionId.
    /// </summary>
    private void SyncSelectedMissionFromOptions()
    {
        if (_missionId == Guid.Empty)
        {
            _missionDisplay = string.Empty;
            return;
        }

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _missionId);
        if (m is null)
        {
            return;
        }

        _missionDisplay = m.DisplayMisssion ?? string.Empty;
    }

    //======================================================================
    // Save / Close
    //======================================================================

    /// <summary>Перевірка валідності: джерело + місія.</summary>
    private bool CanSave
        => !string.IsNullOrWhiteSpace(_sourceDocNo)
           && _missionId != Guid.Empty;

    /// <summary>Зберігає оновлення групи та закриває drawer.</summary>
    private async Task Save()
    {
        try
        {
            var missionDisplay = string.IsNullOrWhiteSpace(_missionDisplay) ? "—" : _missionDisplay;

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

    /// <summary>Закриває drawer.</summary>
    private Task Close()
        => IsOpenChanged.InvokeAsync(false);
}
