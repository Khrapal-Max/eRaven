//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetShell
//-----------------------------------------------------------------------------

using DocumentFormat.OpenXml.Spreadsheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetShell : IDisposable
{
    // ====================================
    // DI
    // ====================================
    [Inject] public IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetMonthPerPersonDto>> Query { get; set; } = default!;

    // ====================================
    // UI
    // ====================================
    private bool _loading;
    private string? _error;
    private bool _personModalOpen;

    private int _year;
    private int _month;
    private int _daysInMonth;

    private string? _search;

    private TimesheetMonthPerPersonDto? _selected;
    private TimesheetMonthPerPersonDto? _personModalPerson;
    private IReadOnlyList<TimesheetMonthPerPersonDto>? _rows;

    // O(1) lookup по клітинках: PersonId -> ((day,lane) -> code)
    private readonly Dictionary<Guid, Dictionary<(int Day, TimesheetLane Lane), string>> _cellIndex = [];

    // ===============================
    // Lifecycle
    // ===============================
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
            _daysInMonth = DateTime.DaysInMonth(_year, _month);

            _rows = await Query.HandleAsync(new GetTimesheetMonthQuery(
                Year: _year,
                Month: _month,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));

            RebuildCellIndex(_rows);
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

    // ===============================
    // Actions
    // ===============================
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

        // мінімально: не стріляємо на кожен символ — тільки якщо >=2 або очистили
        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private void OpenPersonModal(TimesheetMonthPerPersonDto r)
    {
        _personModalPerson = r;
        _personModalOpen = true;
    }

    private void ClosePersonModal()
    {
        _personModalOpen = false;
        _personModalPerson = null;
    }

    private string GetCode(TimesheetMonthPerPersonDto r, int day, TimesheetLane lane)
    {
        if (day < 1 || day > _daysInMonth) return string.Empty;

        if (_cellIndex.TryGetValue(r.PersonId, out var map) &&
            map.TryGetValue((day, lane), out var code))
            return code ?? string.Empty;

        return string.Empty;
    }

    private void RebuildCellIndex(IReadOnlyList<TimesheetMonthPerPersonDto>? rows)
    {
        _cellIndex.Clear();
        if (rows is null) return;

        foreach (var r in rows)
        {
            var map = new Dictionary<(int, TimesheetLane), string>(_daysInMonth * 2);

            var days = r.Timesheet?.Days;
            if (days is not null)
            {
                foreach (var d in days)
                    map[(d.Day, d.Lane)] = d.Code ?? string.Empty;
            }

            _cellIndex[r.PersonId] = map;
        }
    }

    private static string ShortCode(string code)
    {
        var s = (code ?? "").Trim();
        if (s.Length <= 4) return s;

        // якщо є пробіл — беремо першу “частину”
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

    // Безпечно: не прив’язуємось до конкретних enum-значень компілятором
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
        // пріоритет: alert > nb > 30 > vac > task > other > empty
        var m = (main ?? "").Trim().ToUpperInvariant();
        var t = (task ?? "").Trim().ToUpperInvariant();

        if (m.Length == 0 && t.Length == 0) return "ts-cell ts-cell--empty";

        // якщо є task — можна окремо позначити, але не перебивати alert/nb
        bool hasTask = t.Length > 0;

        if (m is "100" or "F100" || t is "100" or "F100") return "ts-cell ts-cell--alert" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "НБ") return "ts-cell ts-cell--nb" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "30") return "ts-cell ts-cell--30" + (hasTask ? " ts-cell--has-task" : "");
        if (m == "ВП") return "ts-cell ts-cell--vac" + (hasTask ? " ts-cell--has-task" : "");

        if (hasTask) return "ts-cell ts-cell--task";
        return "ts-cell ts-cell--other";
    }

    public void Dispose()
    {
        _rows = [];
        GC.SuppressFinalize(this);
    }
}
