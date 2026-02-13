//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetWeekShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetWeekShell : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetTimesheetRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>> GetRangeHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // UI state
    //======================================================================
    private bool _loading;
    private string? _search;

    // AnchorDate = "сьогодні" (логіка створення подій)
    private DateOnly _anchorDate = DateOnly.FromDateTime(DateTime.Today);

    // вікно -2/+4 (7 днів)
    private readonly int _offsetFrom = 2;
    private readonly int _offsetTo = 4;

    private DateOnly _fromDate;
    private DateOnly _toDate;
    private int _anchorIndex;

    private List<DateOnly> _days = [];

    private IReadOnlyList<TimesheetPersonRangeRowDto>? _rows;
    private TimesheetPersonRangeRowDto? _selected;
    private CancellationTokenSource? _reloadCts;

    //======================================================================
    // Drawers
    //======================================================================
    private bool _drawerOpen;
    private Guid _drawerPersonId;
    private Guid _drawerCurrentCodeId;
    private string _drawerCurrentCode = string.Empty;
    private string? _drawerPersonLabel;

    private bool _personDrawerOpen;
    private TimesheetPersonMonthRowDto? _personDrawerPerson;

    //======================================================================
    // Lifecycle
    //======================================================================
    protected override async Task OnInitializedAsync()
        => await ReloadAsync();

    //======================================================================
    // Data loading
    //======================================================================
    private async Task ReloadAsync()
    {
        _reloadCts?.Cancel();
        _reloadCts?.Dispose();
        _reloadCts = new CancellationTokenSource();
        var ct = _reloadCts.Token;

        _loading = true;
        await InvokeAsync(StateHasChanged); // показати overlay одразу

        _fromDate = _anchorDate.AddDays(-_offsetFrom);
        _toDate = _anchorDate.AddDays(_offsetTo);

        var days = _toDate.DayNumber - _fromDate.DayNumber + 1;
        _anchorIndex = _anchorDate.DayNumber - _fromDate.DayNumber;
        _days = [.. Enumerable.Range(0, days).Select(i => _fromDate.AddDays(i))];

        try
        {
            var res = await GetRangeHandler.HandleAsync(new GetTimesheetRangeQuery(
                From: _fromDate,
                To: _toDate,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ), ct);

            if (ct.IsCancellationRequested) return;

            _rows = res; // важливо: НЕ чистимо _rows на старті — старі дані лишаються
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
            // не обнуляй _rows — інакше знову флеш
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                _loading = false;
        }
    }

    //======================================================================
    // Date navigation
    //======================================================================
    private Task PrevDay()
    {
        _anchorDate = _anchorDate.AddDays(-1);
        return ReloadAsync();
    }

    private Task NextDay()
    {
        _anchorDate = _anchorDate.AddDays(1);
        return ReloadAsync();
    }

    private Task GoToday()
    {
        _anchorDate = DateOnly.FromDateTime(DateTime.Today);
        return ReloadAsync();
    }

    //======================================================================
    // Search
    //======================================================================
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        // авто-reload: пусто або >=2 символи
        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    //======================================================================
    // Actions
    //======================================================================
    private bool CanOpenTransition(TimesheetPersonRangeRowDto r)
    {
        // CurrentCodeId беремо з anchor-колонки.
        var anchor = GetAnchorDay(r);

        // якщо "НБ" на anchor — людина не в табелі на цю дату => події заборонені
        if (IsNb(anchor.Code)) return false;

        // додатково можна підсилити правилами життєвого циклу:
        if (r.ExcludedAt.HasValue) return false;

        return true;
    }

    private void OpenTransition(TimesheetPersonRangeRowDto r)
    {
        if (!CanOpenTransition(r)) return;

        var anchor = GetAnchorDay(r);

        _drawerPersonId = r.PersonId;
        _drawerCurrentCodeId = anchor.CodeId;
        _drawerCurrentCode = anchor.Code;
        _drawerPersonLabel = $"{r.Rank} {r.FullName} ({r.RNOKPP})".Trim();

        _drawerOpen = true;
    }

    private void OpenPersonDrawer(TimesheetPersonRangeRowDto r)
    {
        // Адаптер RangeRow -> MonthRow (drawer так очікує)
        var codes = r.Days.Select(d => (d.Code ?? "").Trim()).ToArray();
        var refs = r.Days.Select(d => string.IsNullOrWhiteSpace(d.Reference) ? null : d.Reference.Trim()).ToArray();

        _personDrawerPerson = new TimesheetPersonMonthRowDto(
            PersonId: r.PersonId,
            FullName: r.FullName,
            RNOKPP: r.RNOKPP,
            Rank: r.Rank,
            Position: r.Position,
            EnrollmentKind: r.EnrollmentKind,
            EnrolledAt: r.EnrolledAt,
            ExcludedAt: r.ExcludedAt,
            Codes: codes,
            Referenses: refs
        );

        _personDrawerOpen = true;
    }

    private void ClosePersonDrawer()
    {
        _personDrawerOpen = false;
        _personDrawerPerson = null;
    }

    private TimesheetRangeDayDto GetAnchorDay(TimesheetPersonRangeRowDto r)
    {
        // надійніше ніж індекс: шукаємо день по Date
        var hit = r.Days.FirstOrDefault(x => x.Date == _anchorDate);
        if (hit is not null) return hit;

        return new TimesheetRangeDayDto(
            Date: _anchorDate,
            CodeId: Guid.Empty,
            Code: TimesheetSystemCodes.NotInTimesheet,
            Reference: null);
    }

    //======================================================================
    // UI helpers
    //======================================================================
    private static string DowShort(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Нд",
        _ => ""
    };

    private static string GetSign(EnrollmentKind? kind) => kind switch
    {
        EnrollmentKind.Unit => "ШТ",
        EnrollmentKind.AttachedByList => "НК",
        EnrollmentKind.AttachedByOrder => "БР",
        _ => "—"
    };

    private static string ShortCode(string code)
    {
        var s = (code ?? "").Trim();
        if (s.Length == 0) return "";

        if (s.Length <= 4) return s;

        var sp = s.IndexOf(' ');
        if (sp > 0) s = s[..sp];

        return s.Length <= 4 ? s : s[..4] + "…";
    }

    private static bool IsAlert(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c == TimesheetSystemCodes.DoesTheCombatTask || c == TimesheetSystemCodes.InjuryFact;
    }

    private static bool IsNb(string? code)
        => string.Equals((code ?? "").Trim(), TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase);

    private static string CellClass(string? code, bool isAnchor)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();

        // контрастні "subtle" фони — тільки bootstrap
        var baseCls = c switch
        {
            var x when x == TimesheetSystemCodes.NotInTimesheet => "bg-info-subtle",
            TimesheetSystemCodes.ReadyToCombatTask => "bg-secondary-subtle",
            TimesheetSystemCodes.Leave
                or TimesheetSystemCodes.LeaveSickness
                or TimesheetSystemCodes.LeaveWound => "bg-success-subtle",
            var x when IsAlert(x) => "bg-danger-subtle",
            _ => "bg-warning-subtle"
        };

        // активна колонка: рамка + легка тінь (контраст без CSS)
        var anchorCls = isAnchor ? " border border-2 border-primary shadow-sm" : "";

        return baseCls + anchorCls;
    }

    private static string CodeTextClass(string? code, bool isAnchor)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();

        // текст під фон (emphasis — bootstrap 5.3)
        var text = c switch
        {
            var x when x == TimesheetSystemCodes.NotInTimesheet => "text-info-emphasis",
            TimesheetSystemCodes.ReadyToCombatTask => "text-secondary-emphasis",
            TimesheetSystemCodes.Leave
                or TimesheetSystemCodes.LeaveSickness
                or TimesheetSystemCodes.LeaveWound => "text-success-emphasis",
            var x when IsAlert(x) => "text-danger-emphasis",
            _ => "text-warning-emphasis"
        };

        // читабельність
        var weight = isAnchor ? " fw-bold" : " fw-semibold";
        return "small " + text + weight;
    }
}
