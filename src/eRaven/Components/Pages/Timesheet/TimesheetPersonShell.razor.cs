//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetPersonShell
{

    [Inject] public IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto> Query { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Parameter] public Guid PersonId { get; set; }

    private bool _loading;
    private string? _error;

    private int _year;
    private int _month;

    private TimesheetPersonMonthDto? _dto;

    private readonly string[] _weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];

    private int _offset;
    private int _gridCellCount;

    private Dictionary<(int Day, TimesheetLane Lane), string> _dayMap = [];

    protected override async Task OnParametersSetAsync()
    {
        ReadYearMonthFromQueryString();
        await ReloadAsync();
    }

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

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _dto = await Query.HandleAsync(new GetTimesheetPersonMonthQuery(
                PersonId: PersonId,
                Year: _year,
                Month: _month));

            BuildDayIndex(_dto);
            BuildCalendarGrid(_dto.Year, _dto.Month, _dto.DaysInMonth);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _dto = null;
            _dayMap = [];
            _offset = 0;
            _gridCellCount = 0;
        }
        finally
        {
            _loading = false;
        }
    }

    private void BuildDayIndex(TimesheetPersonMonthDto dto)
    {
        var map = new Dictionary<(int, TimesheetLane), string>(dto.DaysInMonth * 2);

        foreach (var d in dto.Timesheet.Days)
            map[(d.Day, d.Lane)] = d.Code ?? "";

        _dayMap = map;
    }

    private string GetDayCode(int day, TimesheetLane lane)
        => _dayMap.TryGetValue((day, lane), out var code)
            ? (code ?? "")
            : (lane == TimesheetLane.Main ? "НБ" : "");

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

    private async Task OnYearChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var y))
            return;

        y = Math.Clamp(y, 2000, 2100);
        NavigateToMonth(y, _month);
        await Task.CompletedTask;
    }

    private async Task OnMonthChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var m))
            return;

        m = Math.Clamp(m, 1, 12);
        NavigateToMonth(_year, m);
        await Task.CompletedTask;
    }

    private void NavigateToMonth(int year, int month)
    {
        _year = year;
        _month = month;

        var url = Nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["year"] = _year,
            ["month"] = _month
        });

        // навігація оновить OnParametersSetAsync і перезавантажить дані
        Nav.NavigateTo(url);
    }
}
