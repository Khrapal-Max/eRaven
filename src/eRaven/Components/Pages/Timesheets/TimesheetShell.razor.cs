//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheets;

/// <summary>
/// UI shell сторінки місячного табеля.
/// Показує "факт" для осіб, які входять в табель в межах обраного місяця.
/// </summary>
public partial class TimesheetShell : ComponentBase, IDisposable
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Read-query для побудови місячної матриці табеля.
    /// </summary>
    [Inject]
    public IQueryHandler<GetTimesheetsMonthQuery, IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetMonthQueryHandler { get; set; } = default!;

    //======================================================================
    // State
    //======================================================================

    private bool _loading;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private string? _search;

    private EnrollmentKindDto? _filterKind;
    private bool _filterExcludedOnly;

    private IReadOnlyList<TimesheetPersonRangeRowDto>? _rows;
    private IReadOnlyList<TimesheetPersonRangeRowDto>? _visibleRows;
    private readonly List<TimesheetPersonRangeRowDto> _filteredRows = [];
    private TimesheetPersonRangeRowDto? _selected;

    private bool _personDrawerOpen;
    private TimesheetPersonInfoDto? _personDrawerPerson;

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>
    /// Ініціалізує значення року/місяця поточною датою та завантажує дані.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;
        _daysInMonth = DateTime.DaysInMonth(_year, _month);

        await ReloadAsync();
    }

    //======================================================================
    // Data loading
    //======================================================================

    /// <summary>
    /// Перезавантажує дані місячної матриці табеля.
    /// </summary>
    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _daysInMonth = DateTime.DaysInMonth(_year, _month);

            _rows = await GetTimesheetMonthQueryHandler.HandleAsync(new GetTimesheetsMonthQuery(
                Year: _year,
                Month: _month,
                Search: NormalizeSearch(_search)
            ));

            ApplyClientFilters();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Нормалізує пошуковий рядок: null якщо порожній/пробіли.
    /// </summary>
    private static string? NormalizeSearch(string? search)
        => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    //======================================================================
    // UI handlers
    //======================================================================

    /// <summary>
    /// Зміна року (input[type=number]).
    /// </summary>
    private async Task OnYearChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var y))
            return;

        _year = Math.Clamp(y, 2000, 2100);
        await ReloadAsync();
    }

    /// <summary>
    /// Зміна місяця (select).
    /// </summary>
    private async Task OnMonthChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var m))
            return;

        _month = Math.Clamp(m, 1, 12);
        await ReloadAsync();
    }

    /// <summary>
    /// Пошук: перезавантажуємо дані коли:
    /// - рядок очистили, або
    /// - довжина >= 2 (щоб не бити БД на 1 символ).
    /// </summary>
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    //======================================================================
    // Filters
    //======================================================================

    private void SetFilterAll()
    {
        if (_filterKind is null && !_filterExcludedOnly)
            return;

        _filterKind = null;
        _filterExcludedOnly = false;
        ApplyClientFilters();
    }

    private void SetFilterKind(EnrollmentKindDto kind)
    {
        if (_filterKind == kind && !_filterExcludedOnly)
            return;

        _filterKind = kind;
        _filterExcludedOnly = false;
        ApplyClientFilters();
    }

    private void SetFilterExcluded()
    {
        if (_filterExcludedOnly)
            return;

        _filterKind = null;
        _filterExcludedOnly = true;
        ApplyClientFilters();
    }

    private string GetFilterAllButtonClass()
        => _filterKind is null && !_filterExcludedOnly ? "btn-secondary" : "btn-outline-secondary";

    private string GetFilterKindButtonClass(EnrollmentKindDto kind)
        => _filterKind == kind && !_filterExcludedOnly ? "btn-secondary" : "btn-outline-secondary";

    private string GetFilterExcludedButtonClass()
        => _filterExcludedOnly ? "btn-secondary" : "btn-outline-secondary";

    private void ApplyClientFilters()
    {
        if (_rows is null || _rows.Count == 0)
        {
            _filteredRows.Clear();
            _visibleRows = [];
            return;
        }

        if (_filterKind is null && !_filterExcludedOnly)
        {
            _filteredRows.Clear();
            _visibleRows = _rows;
            return;
        }

        _filteredRows.Clear();

        for (var i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];

            if (MatchesFilter(r.Person.EnrollmentKindDto))
                _filteredRows.Add(r);
        }

        _visibleRows = _filteredRows;
    }

    private bool MatchesFilter(EnrollmentKindDto kind)
    {
        if (_filterExcludedOnly)
            return kind is not (EnrollmentKindDto.Unit or EnrollmentKindDto.AttachedByOrder or EnrollmentKindDto.AttachedByList);

        return _filterKind is null || kind == _filterKind.Value;
    }

    //======================================================================
    // Drawer
    //======================================================================

    /// <summary>
    /// Відкриває drawer з деталями по особі (місячний контекст).
    /// </summary>
    private void OpenPersonDrawer(TimesheetPersonRangeRowDto r)
    {
        _personDrawerPerson = r.Person;
        _personDrawerOpen = true;
    }

    /// <summary>
    /// Закриває drawer та очищає вибір.
    /// </summary>
    private void ClosePersonDrawer()
    {
        _personDrawerOpen = false;
        _personDrawerPerson = null;
    }

    //======================================================================
    // Helpers: formatting / classification
    //======================================================================

    /// <summary>
    /// Повертає код для дня (1-based) з масиву кодів.
    /// Повертає "" якщо масив null або day поза межами.
    /// </summary>
    private static TimesheetDaySnapshotDto? GetDay(IReadOnlyList<TimesheetDaySnapshotDto>? days, int day)
    {
        if (days is null) return null;
        var idx = day - 1;
        if (idx < 0 || idx >= days.Count) return null;
        return days[idx];
    }

    /// <summary>
    /// Перетворює ПІБ у формат "Прізвище І.П."
    /// </summary>
    private static string ToInitials(string fullName)
    {
        var s = (fullName ?? "").Trim();
        if (s.Length == 0) return "";

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0];

        var last = parts[0];
        var firstI = parts.Length >= 2 ? char.ToUpperInvariant(parts[1][0]) + "." : "";
        var midI = parts.Length >= 3 ? char.ToUpperInvariant(parts[2][0]) + "." : "";

        return $"{last} {firstI}{midI}".Trim();
    }

    /// <summary>
    /// Повертає коротку мітку типу зарахування.
    /// </summary>
    private static string GetSign(EnrollmentKindDto kind)
        => kind switch
        {
            EnrollmentKindDto.Unit => "ШТ",
            EnrollmentKindDto.AttachedByList => "НАКАЗ",
            EnrollmentKindDto.AttachedByOrder => "БР",
            _ => "ВКЛ"
        };

    /// <summary>
    /// CSS-клас клітинки залежно від основного (Main) коду.
    /// NB ("НБ") у UI — derived стан (немає активного entry), але в матриці ми його показуємо як дефолт.
    /// </summary>
    private static string GetCellClass(TimesheetDaySnapshotDto? snap)
    {
        if (snap is null) return "ts-cell--empty";

        return snap.UiStyle switch
        {
            TimesheetUiStyleDto.NotInTimesheet => "ts-cell--nb",
            TimesheetUiStyleDto.Warning => "ts-cell--warning",
            TimesheetUiStyleDto.Ready => "ts-cell--ready",
            TimesheetUiStyleDto.Danger => "ts-cell--danger",
            TimesheetUiStyleDto.SystemFact => "ts-cell--system",
            _ => "ts-cell--other"
        };
    }

    /// <summary>
    /// Чи є код "alert" (підсвічування + показ Reference у tooltip).
    /// </summary>
    private static bool ShowReference(TimesheetDaySnapshotDto? snap)
        => snap is not null
           && !string.IsNullOrWhiteSpace(snap.Reference)
           && snap.UiStyle == TimesheetUiStyleDto.Danger;

    private static string GetCellTitle(string code, TimesheetDaySnapshotDto? snap)
    {
        var main = (code ?? "").Trim();
        if (main.Length == 0) return string.Empty;

        if (ShowReference(snap))
            return $"{main} · {snap!.Reference}";

        if (snap?.UiStyle == TimesheetUiStyleDto.SystemFact)
            return $"{main} · Системний факт";

        return main;
    }

    //======================================================================
    // IDisposable
    //======================================================================

    /// <summary>
    /// Очищає важкий стан сторінки (навігація/GC).
    /// </summary>
    public void Dispose()
    {
        _rows = [];
        _visibleRows = [];
        _filteredRows.Clear();
        _personDrawerPerson = null;
        GC.SuppressFinalize(this);
    }
}
