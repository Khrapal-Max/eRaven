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

public partial class TimesheetShell : IDisposable
{
    [Inject] public IQueryHandler<GetTimesheetMonthQuery, TimesheetMonthGridDto> Query { get; set; } = default!;

    private bool _loading;
    private string? _error;
    private bool _personDrawerOpen;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private string? _search;

    private TimesheetMonthGridDto? _model;
    private TimesheetMonthPersonRowDto? _selected;
    private TimesheetMonthPersonRowDto? _personDrawerPerson;

    protected override async Task OnInitializedAsync()
    {
        var today = DateTime.Today;
        _year = today.Year;
        _month = today.Month;
        _daysInMonth = DateTime.DaysInMonth(_year, _month);

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _model = await Query.HandleAsync(new GetTimesheetMonthQuery(
                Year: _year,
                Month: _month,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));

            _daysInMonth = _model.DaysInMonth;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnYearChanged(ChangeEventArgs e)
    {
        if (int.TryParse(Convert.ToString(e.Value), out var y))
        {
            _year = Math.Clamp(y, 2000, 2100);
            await ReloadAsync();
        }
    }

    private async Task OnMonthChanged(ChangeEventArgs e)
    {
        if (int.TryParse(Convert.ToString(e.Value), out var m))
        {
            _month = Math.Clamp(m, 1, 12);
            await ReloadAsync();
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private void OpenPersonDrawer(TimesheetMonthPersonRowDto r)
    {
        _personDrawerPerson = r;
        _personDrawerOpen = true;
    }

    private void ClosePersonDrawer()
    {
        _personDrawerOpen = false;
        _personDrawerPerson = null;
    }

    private static string ShortCode(string code)
    {
        var s = (code ?? "").Trim();
        if (s.Length <= 4) return s;

        var sp = s.IndexOf(' ');
        if (sp > 0) s = s[..sp];

        return s.Length <= 4 ? s : s[..4] + "…";
    }

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

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВКЛ"
        };

    private static string GetCellClass(string? main, string? task)
    {
        var m = (main ?? "").Trim().ToUpperInvariant();
        var t = (task ?? "").Trim().ToUpperInvariant();

        if (m.Length == 0 && t.Length == 0) return "ts-cell ts-cell--empty";

        var hasTask = t.Length > 0;

        if (m is "100" or "F100" || t is "100" or "F100") return "ts-cell ts-cell--alert" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "НБ") return "ts-cell ts-cell--nb" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "30") return "ts-cell ts-cell--30" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "ВП") return "ts-cell ts-cell--vac" + (hasTask ? " ts-cell--has-task" : "");

        if (hasTask) return "ts-cell ts-cell--task";
        return "ts-cell ts-cell--other";
    }

    public void Dispose()
    {
        _model = null;
        GC.SuppressFinalize(this);
    }
}
