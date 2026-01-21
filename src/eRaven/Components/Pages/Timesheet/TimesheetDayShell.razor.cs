//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetDayShell
{
    [Inject] public IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>> Query { get; set; } = default!;

    private bool _loading;
    private string? _error;

    private DateOnly _date;
    private string? _search;
    private string? _kind; // string, щоб не привʼязуватись до enum в markup
    private bool _activeOnly = false;

    private IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>? _rows;
    private TimesheetDayPerPersonCurrentStateDto? _selected;

    private bool _modalOpen;
    private TimesheetDayPerPersonCurrentStateDto? _modalRow;

    protected override async Task OnInitializedAsync()
    {
        _date = DateOnly.FromDateTime(DateTime.Today);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            EnrollmentKind? parsedKind = _kind switch
            {
                "Unit" => EnrollmentKind.Unit,
                "AttachedByList" => EnrollmentKind.AttachedByList,
                "AttachedByOrder" => EnrollmentKind.AttachedByOrder,
                _ => null
            };

            _rows = await Query.HandleAsync(new GetTimesheetDayQuery(
                Date: _date,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim(),
                EnrollmentKind: parsedKind,
                ActiveOnly: _activeOnly
            ));
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

    private async Task OnDateChanged(ChangeEventArgs e)
    {
        var s = Convert.ToString(e.Value);
        if (DateOnly.TryParse(s, out var d))
        {
            _date = d;
            await ReloadAsync();
        }
    }

    private async Task OnKindChanged(ChangeEventArgs e)
    {
        _kind = Convert.ToString(e.Value);
        await ReloadAsync();
    }

    private async Task OnActiveOnlyChanged(ChangeEventArgs e)
    {
        _activeOnly = Convert.ToBoolean(e.Value);
        await ReloadAsync();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private void OpenModal(TimesheetDayPerPersonCurrentStateDto r)
    {
        _modalRow = r;
        _modalOpen = true;
    }

    private void CloseModal()
    {
        _modalOpen = false;
        _modalRow = null;
    }

    private static string ShortCode(string? code)
    {
        var s = (code ?? "").Trim();
        if (s.Length <= 4) return s;

        var sp = s.IndexOf(' ');
        if (sp > 0) s = s[..sp];

        return s.Length <= 4 ? s : s[..4] + "…";
    }

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВИКЛ"
        };

    private static string GetCellClassForCode(string? code, TimesheetLane lane)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();

        if (c.Length == 0) return "ts-cell ts-cell--empty";
        if (c == "НБ") return "ts-cell ts-cell--nb";
        if (c == "30") return "ts-cell ts-cell--30";
        if (c == "ВП") return "ts-cell ts-cell--vac";
        if (c is "100" or "F100") return "ts-cell ts-cell--alert";

        if (lane == TimesheetLane.Task) return "ts-cell ts-cell--task";
        return "ts-cell ts-cell--other";
    }
}
