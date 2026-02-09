//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonShell
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace eRaven.Components.Pages.Timesheet;

/// <summary>
/// Сторінка табеля конкретної особи за місяць:
/// - календар (derived з day-codes: Person.Codes),
/// - список записів (source of truth: Entries).
/// Планів/Task-рівня немає: табель = факт.
/// </summary>
public partial class TimesheetPersonShell : ComponentBase
{
    //======================================================================
    // DI + Route params
    //======================================================================

    /// <summary>
    /// Read-query: місяць конкретної особи (day-codes + entries).
    /// </summary>
    [Inject] public IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto?> Query { get; set; } = default!;

    /// <summary>
    /// Команда переходу стану для створення події (transition).
    /// </summary>
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand> Handler { get; set; } = default!;

    [Inject] public NavigationManager Nav { get; set; } = default!;

    [Inject] public ToastService ToastService { get; set; } = default!;

    /// <summary>
    /// Ідентифікатор особи з маршруту.
    /// </summary>
    [Parameter] public Guid PersonId { get; set; }

    //======================================================================
    // State
    //======================================================================

    private bool _loading;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private TimesheetPersonMonthDto? _dto;

    // Calendar grid:
    private readonly string[] _weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];
    private int _offset;
    private int _gridCellCount;

    // Source of truth (entries for the month)
    private List<TimesheetPersonEntryRowDto> _entries = [];

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <summary>
    /// Викликається при зміні параметрів (PersonId / query string).
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        ReadYearMonthFromQueryString();
        await ReloadAsync();
    }

    //======================================================================
    // Query string (year/month)
    //======================================================================

    /// <summary>
    /// Зчитує year/month з query string. Якщо нема — ставить поточну дату.
    /// </summary>
    private void ReadYearMonthFromQueryString()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var q = QueryHelpers.ParseQuery(uri.Query);

        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;

        if (q.TryGetValue("year", out var yv) && int.TryParse(yv.FirstOrDefault(), out var y))
            _year = Math.Clamp(y, 2000, 2100);

        if (q.TryGetValue("month", out var mv) && int.TryParse(mv.FirstOrDefault(), out var m))
            _month = Math.Clamp(m, 1, 12);
    }

    /// <summary>
    /// Навігація на інший місяць шляхом оновлення query string.
    /// </summary>
    private void NavigateToMonth(int year, int month)
    {
        _year = Math.Clamp(year, 2000, 2100);
        _month = Math.Clamp(month, 1, 12);

        var url = Nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["year"] = _year,
            ["month"] = _month
        });

        Nav.NavigateTo(url);
    }

    private Task OnYearChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var y))
            return Task.CompletedTask;

        NavigateToMonth(y, _month);
        return Task.CompletedTask;
    }

    private Task OnMonthChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var m))
            return Task.CompletedTask;

        NavigateToMonth(_year, m);
        return Task.CompletedTask;
    }

    //======================================================================
    // Data loading
    //======================================================================

    /// <summary>
    /// Завантажує табель особи за місяць, перебудовує календар та список entries.
    /// </summary>
    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _dto = await Query.HandleAsync(new GetTimesheetPersonMonthQuery(
                PersonId: PersonId,
                Year: _year,
                Month: _month));

            if (_dto is null)
            {
                _daysInMonth = DateTime.DaysInMonth(_year, _month);
                _entries = [];
                _offset = 0;
                _gridCellCount = 0;
                return;
            }

            _daysInMonth = _dto.DaysInMonth;

            BuildCalendarGrid(_dto.Year, _dto.Month, _dto.DaysInMonth);

            // fact-only: беремо все як є (entries вже тільки фактичні)
            _entries = [.. _dto.Entries];
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
            _dto = null;
            _entries = [];
            _offset = 0;
            _gridCellCount = 0;
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Формує параметри календарної сітки: Monday-first.
    /// </summary>
    private void BuildCalendarGrid(int year, int month, int daysInMonth)
    {
        var first = new DateOnly(year, month, 1);

        // Monday-first offset: Mon=0 .. Sun=6
        var dow = (int)first.DayOfWeek; // Sun=0..Sat=6
        _offset = (dow + 6) % 7;

        var total = _offset + daysInMonth;
        var rows = (int)Math.Ceiling(total / 7.0);
        _gridCellCount = rows * 7;
    }

    //======================================================================
    // Calendar helpers (fact-only)
    //======================================================================
    /// <summary>
    /// Повертає код табеля для конкретного day (1-based) з Person.Codes.
    /// Якщо даних немає — "НБ".
    /// </summary>
    private string GetDayCode(int day)
    {
        if (_dto?.Person.Codes is null) return "НБ";

        var idx = day - 1;
        if (idx < 0 || idx >= _dto.Person.Codes.Count) return "НБ";

        return (_dto.Person.Codes[idx] ?? "").Trim();
    }

    /// <summary>
    /// Повертає довідковий текст для tooltip (1-based) з Person.Referenses.
    /// Має сенс для “alert/fact” кодів (100/ПБД/Ф100).
    /// </summary>
    private string? GetRef(int day)
    {
        if (_dto?.Person.Referenses is null) return null;

        var idx = day - 1;
        if (idx < 0 || idx >= _dto.Person.Referenses.Count) return null;

        var v = _dto.Person.Referenses[idx];
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    /// <summary>
    /// Коди, які вимагають уваги/пояснення (tooltip/ref).
    /// 100 тепер факт — тому лишається тут.
    /// </summary>
    private static bool IsAlert(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c == "100" || c == "ПБД" || c == "Ф100";
    }
}
