/*//-----------------------------------------------------------------------------
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
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

/// <summary>
/// Drawer для створення групи участей (MissionParticipation group) у документі планування.
/// Створення включає:
/// - метадані: джерело (SourceDocNo) + дата початку (From)
/// - вибір місії (MissionId + MissionDisplaySnapshot)
/// - вибір осіб (тільки "вільні" на дату From)
/// </summary>
public partial class CombatGroupCreateDrawer
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Повертає список доступних місій.
    /// Використовуємо GetMissionsQuery(OnlyOpen=true, Search=null, Mode=null).
    /// </summary>
    [Inject] public IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>> LookupMissions { get; set; } = default!;

    /// <summary>
    /// Пошук "вільних" осіб на дату (picker).
    /// Реалізація відсікає зайнятих через MissionParticipationRepository.GetBusyPersonIdsOnDateAsync.
    /// </summary>
    [Inject] public IQueryHandler<SearchFreePersonsForCombatTaskQuery, IReadOnlyList<CombatTaskPersonLookupDto>> SearchFreePersons { get; set; } = default!;

    /// <summary>Toast повідомлення.</summary>
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Callback при збереженні створення групи.
    /// В редакторі документа цей model буде перетворений у StartCombatTaskGroupCommand.
    /// </summary>
    [Parameter] public EventCallback<StartCombatTaskGroupModel> OnCreated { get; set; }

    //======================================================================
    // State: meta
    //======================================================================

    private string _sourceDocNo = string.Empty;
    private DateOnly _from = DateOnly.FromDateTime(DateTime.Now);

    //======================================================================
    // State: missions
    //======================================================================

    private bool _missionsLoading;
    private IReadOnlyList<MissionDto> _missionOptions = [];

    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    //======================================================================
    // State: persons
    //======================================================================

    private const int PersonTake = 80;
    private string _personSearch = string.Empty;
    private IReadOnlyList<CombatTaskPersonLookupDto> _personResults = [];

    /// <summary>
    /// Вибрані особи (PersonId -> lookup snapshot).
    /// Зберігаємо саме DTO, щоб хендлер Start не ходив у PersonRepo за snapshot полями.
    /// </summary>
    private readonly Dictionary<Guid, CombatTaskPersonLookupDto> _selected = [];

    private int _personSearchVersion;

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>
    /// При відкритті drawer-а:
    /// - завантажуємо місії (1 раз)
    /// - скидаємо форму під create
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        await EnsureMissionsLoadedAsync();
        ResetForCreate();
        _personResults = [];
    }

    /// <summary>Скидає стан форми для режиму створення.</summary>
    private void ResetForCreate()
    {
        _sourceDocNo = string.Empty;
        _from = DateOnly.FromDateTime(DateTime.Now);

        ClearMission();

        _personSearch = string.Empty;
        _selected.Clear();
    }

    //======================================================================
    // Missions: loading + grouping + formatting
    //======================================================================

    /// <summary>Гарантує, що місії завантажені.</summary>
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

    /// <summary>Групи місій для optgroup.</summary>
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

    /// <summary>Будує display місії для snapshot (українське представлення).</summary>
    private static string BuildMissionDisplayUa(MissionDto m)
    {
        var area = string.IsNullOrWhiteSpace(m.PositionArea) ? "—" : m.PositionArea;
        var point = string.IsNullOrWhiteSpace(m.NamePoint) ? "—" : m.NamePoint;
        var drone = string.IsNullOrWhiteSpace(m.DroneName) ? "—" : m.DroneName;
        var target = string.IsNullOrWhiteSpace(m.Target) ? "—" : m.Target;

        return $"ПЗ: {area} · {point} · ТЗ: {drone} · Режим: {MissionModeLabel(m.MissionMode)} · {target}";
    }

    /// <summary>Текст option для select (компактно).</summary>
    private static string FormatMissionOption(MissionDto m)
    {
        var area = m.PositionArea;
        var point = m.NamePoint;
        var mode = MissionModeLabel(m.MissionMode);

        var drone = string.IsNullOrWhiteSpace(m.DroneName) ? "—" : TrimTo(m.DroneName!, 20);
        var target = m.Target;

        return $"{area} · {point} · {drone} · ({mode}) · {target}";
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
    /// Обробник зміни місії.
    /// Гарантовано оновлює _missionDisplay для snapshot.
    /// </summary>
    private Task OnMissionChanged(ChangeEventArgs e)
    {
        if (!Guid.TryParse(e.Value?.ToString(), out var id))
            return Task.CompletedTask;

        _missionId = id;

        var m = _missionOptions.FirstOrDefault(x => x.MissionId == _missionId);
        _missionDisplay = m is null ? string.Empty : BuildMissionDisplayUa(m);

        return Task.CompletedTask;
    }

    /// <summary>Очищає вибір місії.</summary>
    private void ClearMission()
    {
        _missionId = Guid.Empty;
        _missionDisplay = string.Empty;
    }

    //======================================================================
    // Persons: search + selection
    //======================================================================

    /// <summary>
    /// Пояснення для empty state в блоці осіб.
    /// </summary>
    private string PersonsEmptyHint
        => (_personSearch ?? string.Empty).Trim().Length < 2
            ? "Введи пошук для вибору осіб."
            : "Немає результатів (можливо, всі знайдені особи вже зайняті на цю дату).";

    /// <summary>
    /// При зміні дати:
    /// - очищаємо вибір
    /// - перезавантажуємо результати (якщо пошук уже введено)
    /// </summary>
    private async Task OnFromChangedAsync()
    {
        _selected.Clear();

        var s = (_personSearch ?? string.Empty).Trim();
        if (s.Length < 2)
        {
            _personResults = [];
            return;
        }

        // одразу оновимо results під нову дату
        _personResults = await SearchFreePersons.HandleAsync(new SearchFreePersonsForCombatTaskQuery(
            Date: _from,
            Search: s,
            Take: PersonTake));
    }

    /// <summary>Debounced пошук осіб (2+ символи).</summary>
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

    /// <summary>
    /// Toggle вибору особи.
    /// NOTE: ChangeEventArgs.Value може бути bool або string ("true"/"false").
    /// </summary>
    private void TogglePerson(CombatTaskPersonLookupDto dto, ChangeEventArgs e)
    {
        var isChecked = e.Value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var b) && b,
            _ => false
        };

        if (isChecked) _selected[dto.PersonId] = dto;
        else _selected.Remove(dto.PersonId);
    }

    //======================================================================
    // Save / Close
    //======================================================================

    /// <summary>Перевірка валідності: джерело + місія + хоча б 1 особа.</summary>
    private bool CanSave
        => !string.IsNullOrWhiteSpace(_sourceDocNo)
           && _missionId != Guid.Empty
           && _selected.Count > 0;

    /// <summary>
    /// Повертає snapshot місії для збереження.
    /// Якщо display не заповнили (наприклад, місія не вибрана) — повертає "—".
    /// </summary>
    private string MissionDisplaySnapshot
        => string.IsNullOrWhiteSpace(_missionDisplay) ? "—" : _missionDisplay;

    /// <summary>Зберігає створення групи та закриває drawer.</summary>
    private async Task Save()
    {
        try
        {
            await OnCreated.InvokeAsync(new StartCombatTaskGroupModel(
                SourceDocNo: _sourceDocNo.Trim(),
                MissionId: _missionId,
                MissionDisplaySnapshot: MissionDisplaySnapshot,
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

    /// <summary>Закриває drawer.</summary>
    private Task Close()
        => IsOpenChanged.InvokeAsync(false);
}
*/