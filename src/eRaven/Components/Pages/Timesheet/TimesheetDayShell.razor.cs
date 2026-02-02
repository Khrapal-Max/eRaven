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
    // Drawer state (create event)
    //======================================================================

    private bool _eventDrawerOpen;
    private TimesheetPersonMonthRowDto? _eventDrawerPerson;
    private DateOnly _eventDrawerDate;

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
    // Actions
    //======================================================================

    /// <summary>
    /// Відкриває drawer створення події (факт/Main) на поточну дату таблиці.
    /// </summary>
    private void OpenEventDrawer(TimesheetPersonDayRowDto row)
    {
        _eventDrawerDate = _date;

        // Мінімальний snapshot для drawer'а (зараз drawer очікує PersonMonthRowDto).
        _eventDrawerPerson = new TimesheetPersonMonthRowDto(
            PersonId: row.PersonId,
            FullName: row.FullName,
            RNOKPP: row.RNOKPP,
            Rank: row.Rank,
            Position: row.Position,
            EnrollmentKind: row.EnrollmentKind,
            EnrolledAt: row.EnrolledAt,
            ExcludedAt: row.ExcludedAt,
            Codes: [],
            Referenses: []
        );

        _eventDrawerOpen = true;
    }

    /// <summary>
    /// Обробляє submit з drawer:
    /// виконує перехід стану табеля та перезавантажує денний зріз.
    /// </summary>
    private async Task HandleCreateEventAsync(TimesheetTransitionCreateDto dto)
    {
        var author = "system"; // TODO: auth user
        var nowUtc = DateTime.UtcNow;

        try
        {
            // Lane прибрали: завжди "факт" (Main) у чистому табелі.
            await CreateEntryHandler.HandleAsync(new TransitionTimesheetStateCommand(
                PersonId: dto.PersonId,
                AnchorDate: dto.AnchorDate,
                InputDate: dto.InputDate,
                NextCode: dto.NextCode,
                Reference: dto.Reference,
                Note: dto.Note,
                Author: author,
                NowUtc: nowUtc
            ));

            Toasts.Success("Збережено", "Подію додано/оновлено");
            await ReloadAsync();
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо зберегти", ex.Message);
            throw;
        }
        catch (ArgumentException ex)
        {
            Toasts.Warning("Невірні дані", ex.Message);
            throw;
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
            throw;
        }
    }

    //======================================================================
    // UI helpers
    //======================================================================

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