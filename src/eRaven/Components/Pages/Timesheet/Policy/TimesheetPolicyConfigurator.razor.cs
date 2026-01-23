//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyConfigurator (code-behind)
//-----------------------------------------------------------------------------
//
// Важливі принципи:
// 1) НБ ("НБ") — системний стан "поза табелем". Він НЕ є подією і не конфігурується.
//    Тому в UI ми його не показуємо (пропускаємо в foreach).
//
// 2) Політика задається для конкретного коду (from):
//    - Allowed transitions (to IDs)
//    - EndDateMeaning (як трактувати дату "по/закінчення")
//    - NextCodeOnEnd (тільки для Main + FirstDayOfNextCode; зазвичай "30")
//
// 3) Переходи конфігуруються тільки в межах одного lane (Main або Task).
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet.Policy;

public partial class TimesheetPolicyConfigurator
{
    // ---------------------------
    // DI
    // ---------------------------
    [Inject] public ITimesheetPolicyRepository PolicyRepo { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    private const string NotInTimesheetCode = "НБ";

    // Codes (loaded from DB). Ми не фільтруємо їх тут спеціально — просто не показуємо NB в UI.
    private List<TimesheetCodeDefinition> _mainCodes = [];
    private List<TimesheetCodeDefinition> _taskCodes = [];

    // Current selection (what we edit)
    private TimesheetCodeDefinition? _selected;
    private TimesheetLane _selectedLane = TimesheetLane.Main;

    // "End date" semantics
    private TimesheetEndDateMeaning _endDateMeaning = TimesheetEndDateMeaning.LastDayOfThisCode;
    private string? _nextCodeOnEnd;

    // Allowed next codes
    private HashSet<Guid> _allowedTo = [];
    private bool _canSave;

    // ---------------------------
    // Lifecycle
    // ---------------------------

    protected override async Task OnInitializedAsync()
    {
        _mainCodes = [.. (await PolicyRepo.GetCodesAsync(TimesheetLane.Main))];
        _taskCodes = [.. (await PolicyRepo.GetCodesAsync(TimesheetLane.Task))];

        // Auto-select first available (Main preferred), skip NB defensively
        var firstMain = _mainCodes.FirstOrDefault(x => !IsNB(x));
        var firstTask = _taskCodes.FirstOrDefault(x => !IsNB(x));

        if (firstMain is not null)
            await SelectAsync(TimesheetLane.Main, firstMain);
        else if (firstTask is not null)
            await SelectAsync(TimesheetLane.Task, firstTask);
    }

    // ---------------------------
    // Selection
    // ---------------------------

    private async Task SelectAsync(TimesheetLane lane, TimesheetCodeDefinition def)
    {
        _selectedLane = lane;
        _selected = def;

        // load current policy values from selected definition
        _endDateMeaning = def.EndDateMeaning;
        _nextCodeOnEnd = def.NextCodeOnEnd;

        // load allowed transitions
        _allowedTo = [.. (await PolicyRepo.GetAllowedNextAsync(def.Id))];

        _canSave = false;
    }

    // ---------------------------
    // UI helpers (NB + business text)
    // ---------------------------

    private static bool IsNB(TimesheetCodeDefinition d)
        => string.Equals(d.Code?.Trim(), NotInTimesheetCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Текст для адміна: як система трактує дату "по/закінчення".
    /// Це пояснення UX-поведінки, а не додаткова логіка збереження.
    /// </summary>
    private string EndMeaningDescription()
    {
        return _endDateMeaning switch
        {
            TimesheetEndDateMeaning.LastDayOfThisCode =>
                "Дата “по” — це останній день цієї події. Наступна подія починається з наступного дня (+1).",

            TimesheetEndDateMeaning.FirstDayOfNextCode =>
                "Дата “по/закінчення” — це дата повернення / effective date. " +
                "На цю дату ставиться наступний код, а поточний код автоматично завершується на день раніше (-1).",

            _ => "Невідоме правило."
        };
    }

    // ---------------------------
    // Right panel targets (same lane only)
    // ---------------------------

    private IEnumerable<TimesheetCodeDefinition> RightPanelTargets()
    {
        if (_selected is null) return [];

        // IMPORTANT: transitions only within current lane
        var list = _selectedLane == TimesheetLane.Main ? _mainCodes : _taskCodes;

        // exclude itself; NB is excluded in markup (and can be excluded here too safely)
        return list.Where(x => x.Id != _selected.Id);
    }

    // ---------------------------
    // Editing transitions
    // ---------------------------

    private void Toggle(Guid toId, bool value)
    {
        if (_selected is null) return;

        if (value) _allowedTo.Add(toId);
        else _allowedTo.Remove(toId);

        _canSave = true;
    }

    private void SelectAll()
    {
        if (_selected is null) return;

        _allowedTo = [.. RightPanelTargets()
            .Where(x => !IsNB(x)) // NB is not configurable
            .Select(x => x.Id)];

        _canSave = true;
    }

    private void ClearAll()
    {
        _allowedTo.Clear();
        _canSave = true;
    }

    // ---------------------------
    // Editing EndDateMeaning / NextCodeOnEnd
    // ---------------------------

    private Task OnMeaningChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();

        if (!Enum.TryParse<TimesheetEndDateMeaning>(raw, ignoreCase: true, out var meaning))
            return Task.CompletedTask;

        _endDateMeaning = meaning;

        // NextCodeOnEnd makes sense only for Main + FirstDayOfNextCode
        if (_selectedLane != TimesheetLane.Main || _endDateMeaning != TimesheetEndDateMeaning.FirstDayOfNextCode)
            _nextCodeOnEnd = null;

        _canSave = true;
        return Task.CompletedTask;
    }

    private Task OnNextCodeChanged(ChangeEventArgs e)
    {
        // shown only for Main + FirstDayOfNextCode, but still safe to parse
        var v = e.Value?.ToString();
        _nextCodeOnEnd = string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        _canSave = true;
        return Task.CompletedTask;
    }

    // ---------------------------
    // Save
    // ---------------------------

    private async Task SaveAsync()
    {
        if (_selected is null) return;

        // Normalize: only meaningful for Main + FirstDayOfNextCode
        var next = _endDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode && _selectedLane == TimesheetLane.Main
            ? (string.IsNullOrWhiteSpace(_nextCodeOnEnd) ? "30" : _nextCodeOnEnd.Trim())
            : null;

        await PolicyRepo.SavePolicyAsync(
            lane: _selectedLane,
            fromCodeId: _selected.Id,
            endDateMeaning: _endDateMeaning,
            nextCodeOnEnd: next,
            allowedToCodeIds: [.. _allowedTo.Where(id => id != Guid.Empty)],
            author: "ui",
            nowUtc: DateTime.UtcNow);

        _canSave = false;

        Toasts.Success("Збережено", $"Політика для {_selectedLane}/{_selected.Code} оновлена.");
    }
}