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

            BuildCalendarGrid(_dto.Year, _dto.Month, _dto.DaysInMonth);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _dto = null;
            _offset = 0;
            _gridCellCount = 0;
        }
        finally
        {
            _loading = false;
        }
    }

    private string GetDayCode(int day, TimesheetLane lane)
    {
        if (_dto is null) return lane == TimesheetLane.Main ? "НБ" : "";

        var idx = day - 1;
        if (idx < 0 || idx >= _dto.DaysInMonth)
            return lane == TimesheetLane.Main ? "НБ" : "";

        var src = lane == TimesheetLane.Main ? _dto.Person.MainCodes : _dto.Person.TaskCodes;

        if (src is null || idx >= src.Count)
            return lane == TimesheetLane.Main ? "НБ" : "";

        return (src[idx] ?? "").Trim();
    }

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

    private Task OnYearChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var y))
            return Task.CompletedTask;

        y = Math.Clamp(y, 2000, 2100);
        NavigateToMonth(y, _month);
        return Task.CompletedTask;
    }

    private Task OnMonthChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var m))
            return Task.CompletedTask;

        m = Math.Clamp(m, 1, 12);
        NavigateToMonth(_year, m);
        return Task.CompletedTask;
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

        Nav.NavigateTo(url);
    }

    // ЧИСТО ДЛЯ ТЕСТА: открытие боковой панели создания события на сегодня
    private bool _eventDrawerOpen;
    private TimesheetPersonMonthRowDto? _eventDrawerPerson;
    private DateOnly _eventDrawerDate;
    private TimesheetLane _eventDrawerLane = TimesheetLane.Main;

    [Inject] public ICommandHandler<TransitionTimesheetStateCommand> Handler { get; set; } = default!;

    private void OpenEventDrawerToday()
    {
        if (_dto?.Person is null) return;

        _eventDrawerPerson = _dto.Person;

        // current date (today)
        _eventDrawerDate = DateOnly.FromDateTime(DateTime.Today);

        _eventDrawerLane = TimesheetLane.Main; // for quick tests
        _eventDrawerOpen = true;
    }

    // optional: if your drawer requires OnSubmit callback
    private async Task HandleCreateEventAsync(TimesheetTransitionCreateDto dto)
    {
        // quick test stub: just close drawer; wiring save can be added later
        var command = new TransitionTimesheetStateCommand(
            PersonId: dto.PersonId,
            Lane: dto.Lane,
            AnchorDate: dto.AnchorDate,
            InputDate: dto.InputDate,
            NextCode: dto.NextCode,
            Reference: dto.Reference,
            Note: dto.Note,
            Author: "test.user",
            NowUtc: DateTime.UtcNow);


        await Handler.HandleAsync(command, default);
        _eventDrawerOpen = false;
    }
}