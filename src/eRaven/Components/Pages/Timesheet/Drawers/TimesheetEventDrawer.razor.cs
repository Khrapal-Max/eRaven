//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEventDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheet.Drawers;

/// <summary>
/// Drawer для виконання policy-driven переходу стану табеля:
/// 1) знаходить активний запис на AnchorDate,
/// 2) підтягує дозволені next-коди з policy,
/// 3) дає користувачу ввести дату “по/закінчення” і next-код,
/// 4) віддає payload назовні (OnSubmit), де вже виконується команда (handler).
/// 
/// Важливо: drawer не змінює дані самостійно — лише збирає валідні параметри.
/// </summary>
public partial class TimesheetEventDrawer
{
    //======================================================================
    // Parameters (inputs/outputs)
    //======================================================================

    /// <summary>Відкритий/закритий стан drawer.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Callback для зміни IsOpen.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Мінімальний snapshot особи для хедера та payload.</summary>
    [Parameter] public TimesheetPersonMonthRowDto? Person { get; set; }

    /// <summary>
    /// Anchor date (дата, по якій визначається “поточний активний стан”).
    /// Зазвичай це дата з day-view або з клітинки календаря.
    /// </summary>
    [Parameter] public DateOnly Date { get; set; }

    /// <summary>
    /// Вихідний payload для створення переходу (на handler’і буде update+insert).
    /// </summary>
    [Parameter] public EventCallback<TimesheetTransitionCreateDto> OnSubmit { get; set; }

    //======================================================================
    // DI
    //======================================================================

    [Inject] public ITimesheetPolicyRepository Policy { get; set; } = default!;
    [Inject] public ITimesheetTimelineRepository Timelines { get; set; } = default!;
    [Inject] public ITimesheetEntryRepository Entries { get; set; } = default!;

    //======================================================================
    // UI state
    //======================================================================

    private bool _busy;
    private bool _loading;
    private bool _wasOpen;

    private EditContext _editContext = default!;

    /// <summary>Коди з довідника policy.</summary>
    private IReadOnlyList<TimesheetCodeDefinition> _allCodes = [];

    /// <summary>Дозволені next-коди для поточного active.</summary>
    private IReadOnlyList<TimesheetCodeDefinition> _allowedCodes = [];

    /// <summary>Таймлайн на AnchorDate (null => поза табелем).</summary>
    private TimesheetTimeline? _timeline;

    /// <summary>Активний запис на AnchorDate (null => нема історії).</summary>
    private TimesheetEntry? _active;

    private TimesheetCodeDefinition? _activeDef;
    private TimesheetCodeDefinition? _nextDef;

    /// <summary>Похідна дата, якою закриємо prev (для підказок UI).</summary>
    private DateOnly _computedPrevLastDay;

    /// <summary>Похідна дата старту next (для підказок UI).</summary>
    private DateOnly _computedNextFrom;

    /// <summary>Форма вводу.</summary>
    protected TransitionModel Model { get; set; } = new();

    /// <summary>
    /// Заборона “Застосувати”, якщо немає базових умов:
    /// - drawer зайнятий/завантажується,
    /// - немає Person,
    /// - немає timeline або active,
    /// - не обраний NextCode.
    /// </summary>
    private bool DisabledSave =>
        _busy ||
        _loading ||
        Person is null ||
        _timeline is null ||
        _active is null ||
        string.IsNullOrWhiteSpace(Model.NextCode);

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>Ініціалізація внутрішнього стану.</summary>
    protected override void OnInitialized() => Reset();

    /// <summary>
    /// Ловимо момент першого відкриття drawer, щоб один раз завантажити дані.
    /// При закритті — очищаємо стан.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await LoadAsync();
            return;
        }

        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }
    }

    //======================================================================
    // Data loading
    //======================================================================

    /// <summary>
    /// Завантажує:
    /// - policy codes,
    /// - timeline на anchor date,
    /// - active entry на anchor date,
    /// - дозволені next-коди,
    /// та ініціалізує модель форми.
    /// </summary>
    private async Task LoadAsync()
    {
        Reset();

        if (Person is null)
            return;

        _loading = true;

        try
        {
            // Base model
            Model.PersonId = Person.PersonId;
            Model.AnchorDate = Date;

            _allCodes = await Policy.GetCodesAsync();

            _timeline = await Timelines.GetTimelineOnDateAsync(Person.PersonId, Date);
            _active = await Entries.GetActiveEntryOnDateAsync(Person.PersonId, Date);

            // No timeline / no active => cannot apply transition
            if (_timeline is null || _active is null)
            {
                _allowedCodes = [];
                Model.InputDate = Date;
                Model.NextCode = "";
                _activeDef = null;
                _nextDef = null;

                RecalcDates();
                _editContext = new EditContext(Model);
                return;
            }

            _activeDef = FindDef(_active.Code);
            _allowedCodes = await GetAllowedCodesAsync();

            // Default input date (interpreted by activeDef meaning)
            Model.InputDate = Date;

            // Default next code: first allowed
            Model.NextCode = _allowedCodes.Count > 0 ? _allowedCodes[0].Code : "";

            _nextDef = FindDef(Model.NextCode);

            RecalcDates();

            _editContext = new EditContext(Model);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Повертає список дозволених next-кодів відносно поточного active-коду.
    /// Якщо policy для from-коду не знайдено або не має transitions — повертаємо всі коди (fail-open для UI).
    /// </summary>
    private async Task<IReadOnlyList<TimesheetCodeDefinition>> GetAllowedCodesAsync()
    {
        if (_active is null || string.IsNullOrWhiteSpace(_active.Code))
            return _allCodes;

        var fromDef = FindDef(_active.Code);
        if (fromDef is null)
            return _allCodes;

        var allowedIds = await Policy.GetAllowedNextAsync(fromDef.Id);

        if (allowedIds.Count == 0)
            return _allCodes;

        return [.. _allCodes.Where(x => allowedIds.Contains(x.Id))];
    }

    //======================================================================
    // UI events
    //======================================================================

    /// <summary>Реакція на зміну InputDate.</summary>
    private void OnInputDateChanged()
    {
        RecalcDates();
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.InputDate)));
    }

    /// <summary>Реакція на зміну NextCode.</summary>
    private void OnNextCodeChanged()
    {
        _nextDef = FindDef(Model.NextCode);
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.NextCode)));
    }

    //======================================================================
    // Date math
    //======================================================================

    /// <summary>
    /// Перераховує “computed” дати закриття prev та старту next
    /// на основі EndDateMeaning активного коду.
    /// </summary>
    private void RecalcDates()
    {
        // Safe defaults for stable UI
        if (_activeDef is null)
        {
            _computedPrevLastDay = Model.InputDate;
            _computedNextFrom = Model.InputDate;
            return;
        }

        if (_activeDef.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
        {
            // input date = last day of current
            _computedPrevLastDay = Model.InputDate;
            _computedNextFrom = Model.InputDate.AddDays(1);
        }
        else
        {
            // input date = first day of next
            _computedNextFrom = Model.InputDate;
            _computedPrevLastDay = Model.InputDate.AddDays(-1);
        }
    }

    //======================================================================
    // Code definition lookup
    //======================================================================

    /// <summary>
    /// Знаходить TimesheetCodeDefinition по коду (case-insensitive, trim).
    /// </summary>
    private TimesheetCodeDefinition? FindDef(string? code)
    {
        var c = Normalize(code);
        if (c.Length == 0) return null;

        return _allCodes.FirstOrDefault(x => Normalize(x.Code) == c);
    }

    //======================================================================
    // Submit / close
    //======================================================================

    /// <summary>
    /// Submit валідної форми:
    /// нормалізує строки і віддає payload назовні.
    /// </summary>
    private async Task SubmitAsync()
    {
        if (_busy || _loading || Person is null)
            return;

        _busy = true;

        try
        {
            Model.NextCode = (Model.NextCode ?? "").Trim();
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Note = TrimOrNull(Model.Note);

            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(new TimesheetTransitionCreateDto(
                    PersonId: Model.PersonId,
                    AnchorDate: Model.AnchorDate,
                    InputDate: Model.InputDate,
                    NextCode: Model.NextCode,
                    Reference: Model.Reference,
                    Note: Model.Note
                ));
            }

            await IsOpenChanged.InvokeAsync(false);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Закриття drawer по кнопці “Скасувати”.
    /// </summary>
    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    /// <summary>
    /// Хук drawer’а: очищає стан при закритті.
    /// </summary>
    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }

    //======================================================================
    // Reset
    //======================================================================

    /// <summary>
    /// Повертає drawer у чистий стан (щоб при наступному відкритті не було “хвостів”).
    /// </summary>
    private void Reset()
    {
        _busy = false;
        _loading = false;

        _allCodes = [];
        _allowedCodes = [];

        _timeline = null;
        _active = null;

        _activeDef = null;
        _nextDef = null;

        Model = new TransitionModel
        {
            AnchorDate = Date,
            InputDate = Date,
            NextCode = ""
        };

        _computedPrevLastDay = Model.InputDate;
        _computedNextFrom = Model.InputDate;

        _editContext = new EditContext(Model);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static string Normalize(string? code) => (code ?? "").Trim().ToUpperInvariant();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}