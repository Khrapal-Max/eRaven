//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

/// <summary>
/// UI shell сторінки місячного табеля.
/// Показує "факт" (MainCodes) для осіб, які входять в табель в межах обраного місяця.
/// </summary>
public partial class TimesheetShell : IDisposable
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Read-query для побудови місячної матриці табеля.
    /// </summary>
    [Inject]
    public IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetPersonMonthRowDto>> Query { get; set; } = default!;

    //======================================================================
    // State
    //======================================================================

    private bool _loading;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private string? _search;

    private IReadOnlyList<TimesheetPersonMonthRowDto>? _rows;
    private TimesheetPersonMonthRowDto? _selected;

    private bool _personDrawerOpen;
    private TimesheetPersonMonthRowDto? _personDrawerPerson;

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

            _rows = await Query.HandleAsync(new GetTimesheetMonthQuery(
                Year: _year,
                Month: _month,
                Search: NormalizeSearch(_search)
            ));
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
    // Drawer
    //======================================================================

    /// <summary>
    /// Відкриває drawer з деталями по особі (місячний контекст).
    /// </summary>
    private void OpenPersonDrawer(TimesheetPersonMonthRowDto r)
    {
        _personDrawerPerson = r;
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
    private static string GetCode(IReadOnlyList<string>? codes, int day)
    {
        if (codes is null) return "";
        var idx = day - 1;
        if (idx < 0 || idx >= codes.Count) return "";
        return (codes[idx] ?? "").Trim();
    }

    /// <summary>
    /// Скорочує код для клітинки (візуально).
    /// Напр. "БВ (СЗЧ)" => "БВ…" або до першого пробілу.
    /// </summary>
    private static string ShortCode(string code)
    {
        var s = (code ?? "").Trim();
        if (s.Length == 0) return "";
        if (s.Length <= 4) return s;

        var sp = s.IndexOf(' ');
        if (sp > 0) s = s[..sp];

        return s.Length <= 4 ? s : s[..4] + "…";
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
    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВКЛ"
        };

    /// <summary>
    /// CSS-клас клітинки залежно від основного (Main) коду.
    /// NB ("НБ") у UI — derived стан (немає активного entry), але в матриці ми його показуємо як дефолт.
    /// </summary>
    private static string GetCellClass(string? main)
    {
        var m = (main ?? "").Trim().ToUpperInvariant();
        if (m.Length == 0) return "ts-cell--empty";

        if (IsAlert(m)) return "ts-cell--alert";
        if (m == "НБ") return "ts-cell--nb";
        if (m == "30") return "ts-cell--30";
        if (m is "ВП" or "ВПХ" or "ВПП") return "ts-cell--vac";

        return "ts-cell--other";
    }

    /// <summary>
    /// Чи є код "alert" (підсвічування + показ Reference у tooltip).
    /// </summary>
    private static bool IsAlert(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c == "100" || c == "ПБД" || c == "Ф100";
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
        _personDrawerPerson = null;
        GC.SuppressFinalize(this);
    }
}
