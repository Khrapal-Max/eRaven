//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonShell
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

// TODO need add void and correction drawer? fs

namespace eRaven.Components.Pages.Timesheets;

/// <summary>
/// Сторінка табеля конкретної особи за місяць:
/// - календар (days: source of truth: DTO.Days)
/// - список записів (entries: source of truth: DTO.Entries)
/// </summary>
public partial class TimesheetPersonShell : ComponentBase
{
    //======================================================================
    // DI + Route params
    //======================================================================

    [Inject] public IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto?> GetTimesheetPersonMonthQueryHandler { get; set; } = default!;

    // NOTE: will be used for correction/void flow.
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand, Guid> TransitionTimesheetStateCommandHandler { get; set; } = default!;

    [Inject] public ToastService ToastService { get; set; } = default!;

    [Parameter] public Guid PersonId { get; set; }

    //======================================================================
    // State
    //======================================================================

    private bool _loading;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private Guid _currentPersonId;

    // header
    private TimesheetPersonInfoDto? _person;
    private DateTime _updated;
    private IReadOnlyList<TimesheetPersonEntryRowDto>? _entries;
    private IReadOnlyList<TimesheetDaySnapshotDto>? _days;

    // Calendar grid:
    private readonly string[] _weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд"];
    private int _offset;
    private int _gridCellCount;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnParametersSetAsync()
    {
        // Default month: current. If person changes - reset selection.
        if (PersonId != _currentPersonId)
        {
            _currentPersonId = PersonId;
            SetCurrentMonth();
        }
        else if (_year == 0 || _month == 0)
        {
            SetCurrentMonth();
        }

        await ReloadAsync();
    }

    private void SetCurrentMonth()
    {
        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;
    }

    private async Task SetMonthAsync(int year, int month)
    {
        _year = Math.Clamp(year, 2000, 2100);
        _month = Math.Clamp(month, 1, 12);
        await ReloadAsync();
    }

    private async Task OnYearChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var y))
            return;

        await SetMonthAsync(y, _month);
    }

    private async Task OnMonthChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(Convert.ToString(e.Value), out var m))
            return;

        await SetMonthAsync(_year, m);
    }

    //======================================================================
    // Data loading
    //======================================================================

    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            var timesheet = await GetTimesheetPersonMonthQueryHandler.HandleAsync(new GetTimesheetPersonMonthQuery(
                PersonId: PersonId,
                Year: _year,
                Month: _month));

            if (timesheet is not null)
            {
                _person = timesheet.Person;
                _updated = timesheet.UpdatedAtUtc;
                _days = timesheet.Days;
                _entries = timesheet.Entries;
            }

            _daysInMonth = DateTime.DaysInMonth(_year, _month);

            if (timesheet is null)
            {
                _days = timesheet?.Days;

                _offset = 0;
                _gridCellCount = 0;
                return;
            }

            // Contract: view/repo returns a ready month matrix (including derived NB).
            if (timesheet.Days.Count != _daysInMonth)
                throw new InvalidOperationException($"Некоректний розмір матриці табеля: очікується {_daysInMonth}, отримано {_days?.Count}.");

            BuildCalendarGrid(_year, _month, _daysInMonth);
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
            _person = null;
            _updated = DateTime.MinValue;
            _days = [];
            _entries = [];
            _offset = 0;
            _gridCellCount = 0;
        }
        finally
        {
            _loading = false;
        }
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

    //======================================================================
    // Calendar helpers (UI mapping is based on UiStyle + IsDerived)
    //======================================================================

    private TimesheetDaySnapshotDto GetDaySnapshot(int day)
        => _days![day - 1];

    private static string GetDayCode(TimesheetDaySnapshotDto snap)
        => (snap.Code ?? string.Empty).Trim();

    private static string GetDayCellClass(TimesheetDaySnapshotDto snap)
    {
        if (snap.IsDerived || snap.UiStyle == TimesheetUiStyleDto.NotInTimesheet)
            return "ts-cell--nb";

        return snap.UiStyle switch
        {
            TimesheetUiStyleDto.Warning => "ts-cell--warning",
            TimesheetUiStyleDto.Ready => "ts-cell--ready",
            TimesheetUiStyleDto.Danger => "ts-cell--danger",
            TimesheetUiStyleDto.SystemFact => "ts-cell--system",
            _ => "ts-cell--other",
        };
    }

    private static string GetDayTitle(TimesheetDaySnapshotDto snap)
    {
        if (snap.IsDerived || snap.UiStyle == TimesheetUiStyleDto.NotInTimesheet)
            return "НБ (derived)";

        if (snap.UiStyle == TimesheetUiStyleDto.Danger)
        {
            // Для аварійних/alert кодів reference важливий.
            var r = string.IsNullOrWhiteSpace(snap.Reference) ? null : snap.Reference.Trim();
            var n = string.IsNullOrWhiteSpace(snap.Note) ? null : snap.Note.Trim();

            if (!string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(n))
                return $"{r} · {n}";

            return r ?? n ?? string.Empty;
        }

        if (snap.UiStyle == TimesheetUiStyleDto.SystemFact)
        {
            var n = string.IsNullOrWhiteSpace(snap.Note) ? null : snap.Note.Trim();
            return n is null ? "Системний факт (⚙️)" : $"Системний факт (⚙️) · {n}";
        }

        return string.IsNullOrWhiteSpace(snap.Note) ? string.Empty : snap.Note.Trim();
    }
}
