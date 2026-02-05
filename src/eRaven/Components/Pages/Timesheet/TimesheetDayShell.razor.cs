//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayShell
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

/// <summary>
/// Сторінка "Табель (стан на день)".
/// Показує поточний стан (факт/Main) на обрану дату для всіх осіб у табелі.
/// 
/// TODO (пізніше): у цій таблиці з'явиться "План" з документів (бойові/планові документи),
/// але зараз це заглушка без бізнес-логіки.
/// </summary>
public partial class TimesheetDayShell
{
    //======================================================================
    // DI
    //======================================================================

    /// <summary>
    /// Read-query: повертає денний зріз табеля.
    /// </summary>
    [Inject] public IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonDayRowDto>> Query { get; set; } = default!;

    /// <summary>
    /// Command-handler: policy-driven transition стану табеля (update+insert).
    /// </summary>
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand> CreateEntryHandler { get; set; } = default!;

    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // UI state
    //======================================================================

    private bool _loading;

    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private string _dateIso = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

    private string? _search;

    private IReadOnlyList<TimesheetPersonDayRowDto>? _rows;
    private TimesheetPersonDayRowDto? _selected;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override async Task OnInitializedAsync()
        => await ReloadAsync();

    //======================================================================
    // Data loading
    //======================================================================

    /// <summary>
    /// Завантажує денний зріз табеля з урахуванням пошуку.
    /// </summary>
    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _rows = await Query.HandleAsync(new GetTimesheetDayQuery(
                Date: _date,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));
        }
        catch (Exception ex)
        {
            _rows = [];
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    //======================================================================
    // Filters
    //======================================================================

    /// <summary>
    /// Зміна дати (input[type=date]) та перезавантаження.
    /// </summary>
    private async Task OnDateChanged(ChangeEventArgs e)
    {
        var s = Convert.ToString(e.Value) ?? "";
        if (DateOnly.TryParse(s, out var d))
        {
            _date = d;
            _dateIso = d.ToString("yyyy-MM-dd");
            await ReloadAsync();
        }
    }

    /// <summary>
    /// Пошук по ПІБ або РНОКПП (мін. 2 символи для автоперезавантаження).
    /// </summary>
    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    //======================================================================
    // UI helpers
    //======================================================================

    /// <summary>
    /// Дозволяє відкривати drawer лише якщо для особи є діючий (активний) timeline на поточну дату сторінки.
    /// Узгодження з правилом p.1: якщо timeline закритий (особа виключена) — вставки заборонені, тому UI також блокуємо.
    /// </summary>
    private bool CanOpenEventDrawer(TimesheetPersonDayRowDto row)
    {
        if (!row.EnrolledAt.HasValue) return false;
        if (row.ExcludedAt.HasValue) return false;          // НЕ активний timeline
        return _date >= row.EnrolledAt.Value;               // дата в межах активного (від EnrolledAt і далі)
    }

    /// <summary>
    /// Повертає коротку “позначку типу” для EnrollmentKind.
    /// </summary>
    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВИКЛ"
        };
}