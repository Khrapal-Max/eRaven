//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetWeekShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheets;

public partial class TimesheetWeekShell : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetTimesheetsRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetRangeQueryHandler { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

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
    private DateOnly _drawerInitialDate;
    private string? _drawerPersonLabel;

    private bool _personDrawerOpen;
    private TimesheetPersonInfoDto? _personDrawerPerson;

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
        await InvokeAsync(StateHasChanged);

        _fromDate = _anchorDate.AddDays(-_offsetFrom);
        _toDate = _anchorDate.AddDays(_offsetTo);

        var days = _toDate.DayNumber - _fromDate.DayNumber + 1;
        _anchorIndex = _anchorDate.DayNumber - _fromDate.DayNumber;
        _days = [.. Enumerable.Range(0, days).Select(i => _fromDate.AddDays(i))];

        try
        {
            var res = await GetTimesheetRangeQueryHandler.HandleAsync(new GetTimesheetsRangeQuery(
                From: _fromDate,
                To: _toDate,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ), ct);

            if (ct.IsCancellationRequested) return;

            _rows = res;
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
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
        // Мінімальна перевірка для операційного табеля:
        // - якщо на операційний день (anchor) derived / NotInTimesheet — людина не в табелі => події заборонені
        var anchor = GetDayAtIndex(r, _anchorIndex);
        if (anchor.IsDerived || anchor.UiStyle == TimesheetUiStyleDto.NotInTimesheet) return false;

        // додатково: якщо виключений — не даємо створювати події
        if (r.Person.ExcludedAt.HasValue) return false;

        return true;
    }

    private void OpenTransition(TimesheetPersonRangeRowDto r)
    {
        if (!CanOpenTransition(r)) return;

        _drawerPersonId = r.Person.PersonId;

        // Drawer сам визначить поточний код і дозволені переходи для обраної дати.
        // Тут передаємо лише стартову дату (за замовчуванням — операційний день).
        _drawerInitialDate = _anchorDate;

        _drawerPersonLabel = $"{r.Person.Rank} {r.Person.FullName} ({r.Person.Rnokpp})".Trim();
        _drawerOpen = true;
    }

    private void OpenPersonDrawer(TimesheetPersonRangeRowDto r)
    {
        _personDrawerPerson = r.Person;
        _personDrawerOpen = true;
    }

    private void ClosePersonDrawer()
    {
        _personDrawerOpen = false;
        _personDrawerPerson = null;
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

    private static string GetSign(EnrollmentKindDto kind) => kind switch
    {
        EnrollmentKindDto.Unit => "ШТ",
        EnrollmentKindDto.AttachedByList => "НК",
        EnrollmentKindDto.AttachedByOrder => "БР",
        _ => "РЕЗЕРВ"
    };

    private TimesheetDaySnapshotDto GetDayAtIndex(TimesheetPersonRangeRowDto r, int i)
    {
        // IMPORTANT: TimesheetViewRepository гарантує “повну матрицю” на запитаний діапазон.
        // Фолбек лишаємо лише як захист від неконсистентних даних.
        if (i >= 0 && i < r.Days.Count) return r.Days[i];

        var d = _days.Count > i && i >= 0 ? _days[i] : _anchorDate;

        return new TimesheetDaySnapshotDto(
            Date: d,
            CodeId: null,
            Code: string.Empty,
            Reference: null,
            Note: null,
            IsDerived: true,
            IsChangePoint: false,
            UiStyle: TimesheetUiStyleDto.NotInTimesheet);
    }

    private static string GetCellTitle(TimesheetDaySnapshotDto d)
    {
        if (d.IsDerived || d.UiStyle == TimesheetUiStyleDto.NotInTimesheet)
        {
            var code = string.IsNullOrWhiteSpace(d.Code) ? "—" : d.Code;
            return $"{code} (derived)";
        }

        if (d.UiStyle == TimesheetUiStyleDto.SystemFact)
            return string.IsNullOrWhiteSpace(d.Note) ? "Системний факт (⚙️)" : $"Системний факт (⚙️): {d.Note}";

        if (d.UiStyle == TimesheetUiStyleDto.Danger && !string.IsNullOrWhiteSpace(d.Reference))
            return d.Reference!;

        return string.Empty;
    }

    private static string GetCellClass(TimesheetDaySnapshotDto d, bool isAnchor)
    {
        var style = (d.IsDerived || d.UiStyle == TimesheetUiStyleDto.NotInTimesheet) ? "ts-cell--nb"
            : d.UiStyle switch
            {
                TimesheetUiStyleDto.Warning => "ts-cell--warning",
                TimesheetUiStyleDto.Ready => "ts-cell--ready",
                TimesheetUiStyleDto.Danger => "ts-cell--danger",
                TimesheetUiStyleDto.SystemFact => "ts-cell--system",
                _ => "ts-cell--other"
            };

        var anchor = isAnchor ? " ts-cell--anchor border border-2 border-primary shadow-sm" : "";

        return "ts-cell " + style + anchor;
    }
}
