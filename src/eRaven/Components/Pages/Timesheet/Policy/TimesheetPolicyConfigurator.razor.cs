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
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet.Policy;

/// <summary>
/// Конфігуратор політики переходів табеля (без lane).
/// 
/// Налаштовує для одного коду (from):
/// - список дозволених наступних кодів (to),
/// - семантику інтерпретації дати завершення,
/// - NextCodeOnEnd (лише коли Meaning = FirstDayOfNextCode).
/// 
/// NB (“НБ”) — системний стан “поза табелем”: не є подією і не конфігурується.
/// </summary>
public partial class TimesheetPolicyConfigurator
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public ITimesheetPolicyRepository PolicyRepo { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Constants
    //======================================================================

    /// <summary>
    /// Системний код “поза табелем”. Не конфігуруємо як подію.
    /// </summary>
    private const string NotInTimesheetCode = "НБ";

    //======================================================================
    // Loaded data
    //======================================================================

    /// <summary>
    /// Довідник кодів (з БД). Порядок задає репозиторій (SortOrder, Code).
    /// </summary>
    private List<TimesheetCodeDefinition> _codes = [];

    //======================================================================
    // Current selection (edit target)
    //======================================================================

    /// <summary>
    /// Поточний обраний код (from), який редагуємо.
    /// </summary>
    private TimesheetCodeDefinition? _selected;

    /// <summary>
    /// Як трактувати введену користувачем “дату по/закінчення”.
    /// </summary>
    private TimesheetEndDateMeaning _endDateMeaning = TimesheetEndDateMeaning.LastDayOfThisCode;

    /// <summary>
    /// Наступний код, який система виставляє на дату повернення (Meaning = FirstDayOfNextCode).
    /// Напр., для відпусток — зазвичай “30”.
    /// </summary>
    private string? _nextCodeOnEnd;

    /// <summary>
    /// Набір дозволених переходів (to) для поточного from.
    /// </summary>
    private HashSet<Guid> _allowedTo = [];

    /// <summary>
    /// Прапорець “є зміни” для кнопки Зберегти.
    /// </summary>
    private bool _canSave;

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>
    /// Початкове завантаження довідника кодів.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        _codes = [.. await PolicyRepo.GetCodesAsync()];
    }

    //======================================================================
    // UI helpers
    //======================================================================

    /// <summary>
    /// Чи є код системним “НБ”.
    /// </summary>
    private static bool IsNB(TimesheetCodeDefinition d)
        => string.Equals(d.Code?.Trim(), NotInTimesheetCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Пояснення для адміністратора: як система трактує дату “по/закінчення”.
    /// Це лише UX-текст, не додаткова логіка збереження.
    /// </summary>
    private string EndMeaningDescription()
        => _endDateMeaning switch
        {
            TimesheetEndDateMeaning.LastDayOfThisCode =>
                "Дата “по” — це останній день цієї події. Наступна подія починається з наступного дня (+1).",

            TimesheetEndDateMeaning.FirstDayOfNextCode =>
                "Дата “по/закінчення” — це дата повернення / effective date. " +
                "На цю дату ставиться наступний код, а поточний код завершується на день раніше (-1).",

            _ => "Невідоме правило."
        };

    //======================================================================
    // Selection
    //======================================================================

    /// <summary>
    /// Обирає код для редагування, завантажує його transitions з БД
    /// та ініціалізує форму значеннями з довідника.
    /// </summary>
    private async Task SelectAsync(TimesheetCodeDefinition code)
    {
        _selected = code;

        _endDateMeaning = code.EndDateMeaning;
        _nextCodeOnEnd = string.IsNullOrWhiteSpace(code.NextCodeOnEnd) ? null : code.NextCodeOnEnd.Trim();

        var allowed = await PolicyRepo.GetAllowedNextAsync(code.Id);
        _allowedTo = [.. allowed];

        _canSave = false;
    }

    /// <summary>
    /// Список target-кодів у правій панелі:
    /// усі коди, крім NB та самого from.
    /// </summary>
    private IEnumerable<TimesheetCodeDefinition> RightPanelTargets()
    {
        if (_selected is null) return [];

        return _codes
            .Where(x => !IsNB(x))
            .Where(x => x.Id != _selected.Id);
    }

    //======================================================================
    // Editing transitions
    //======================================================================

    /// <summary>
    /// Вмикає/вимикає дозволений перехід для toId.
    /// </summary>
    private void Toggle(Guid toId, bool value)
    {
        if (_selected is null) return;

        if (value) _allowedTo.Add(toId);
        else _allowedTo.Remove(toId);

        _canSave = true;
    }

    /// <summary>
    /// Дозволити переходи на всі інші коди (крім NB та самого from).
    /// </summary>
    private void SelectAll()
    {
        if (_selected is null) return;

        _allowedTo = [.. RightPanelTargets().Select(x => x.Id)];
        _canSave = true;
    }

    /// <summary>
    /// Очистити всі дозволені переходи.
    /// </summary>
    private void ClearAll()
    {
        _allowedTo.Clear();
        _canSave = true;
    }

    //======================================================================
    // Editing EndDateMeaning / NextCodeOnEnd
    //======================================================================

    /// <summary>
    /// Зміна правила трактування “дати по/закінчення”.
    /// </summary>
    private Task OnMeaningChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();

        if (!Enum.TryParse<TimesheetEndDateMeaning>(raw, ignoreCase: true, out var meaning))
            return Task.CompletedTask;

        _endDateMeaning = meaning;

        // NextCodeOnEnd має сенс лише для FirstDayOfNextCode
        if (_endDateMeaning != TimesheetEndDateMeaning.FirstDayOfNextCode)
            _nextCodeOnEnd = null;

        _canSave = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Зміна NextCodeOnEnd (використовується тільки при FirstDayOfNextCode).
    /// </summary>
    private Task OnNextCodeChanged(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _nextCodeOnEnd = string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        _canSave = true;
        return Task.CompletedTask;
    }

    //======================================================================
    // Save
    //======================================================================

    /// <summary>
    /// Зберігає політику:
    /// - повністю перезаписує transitions (from → allowedTo),
    /// - оновлює EndDateMeaning та NextCodeOnEnd (за потреби).
    /// </summary>
    private async Task SaveAsync()
    {
        if (_selected is null) return;

        // Normalize NextCodeOnEnd:
        // - тільки для FirstDayOfNextCode
        // - дефолт "30" якщо пусто
        var next = _endDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode
            ? (string.IsNullOrWhiteSpace(_nextCodeOnEnd) ? "30" : _nextCodeOnEnd.Trim())
            : null;

        await PolicyRepo.SavePolicyAsync(
            fromCodeId: _selected.Id,
            endDateMeaning: _endDateMeaning,
            nextCodeOnEnd: next,
            allowedToCodeIds: [.. _allowedTo.Where(id => id != Guid.Empty)],
            author: "ui",
            nowUtc: DateTime.UtcNow);

        _canSave = false;

        Toasts.Success("Збережено", $"Політика для {_selected.Code} оновлена.");
    }
}