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

public partial class TimesheetDayShell
{
    [Inject] public IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonDayRowDto>> Query { get; set; } = default!;
    [Inject] public ICommandHandler<TransitionTimesheetStateCommand> CreateEntryHandler { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    private bool _loading;
    private string? _error;

    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private string _dateIso = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

    private string? _search;

    private IReadOnlyList<TimesheetPersonDayRowDto>? _rows;
    private TimesheetPersonDayRowDto? _selected;

    // drawer
    private bool _eventDrawerOpen;
    private TimesheetPersonMonthRowDto? _eventDrawerPerson;
    private DateOnly _eventDrawerDate;
    private TimesheetLane _eventDrawerLane;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _rows = await Query.HandleAsync(new GetTimesheetDayQuery(
                Date: _date,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _rows = [];
        }
        finally
        {
            _loading = false;
        }
    }

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

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private void OpenEventDrawer(TimesheetPersonDayRowDto row, TimesheetLane lane)
    {
        _eventDrawerLane = lane;
        _eventDrawerDate = _date;

        // Minimal person snapshot to satisfy existing TimesheetEventDrawer signature
        _eventDrawerPerson = new TimesheetPersonMonthRowDto(
            PersonId: row.PersonId,
            FullName: row.FullName,
            RNOKPP: row.RNOKPP,
            Rank: row.Rank,
            Position: row.Position,
            EnrollmentKind: row.EnrollmentKind,
            EnrolledAt: row.EnrolledAt,
            ExcludedAt: row.ExcludedAt,
            MainCodes: [],
            MainRef: [],
            TaskCodes: []
        );

        _eventDrawerOpen = true;
    }

    private async Task HandleCreateEventAsync(TimesheetTransitionCreateDto dto)
    {
        var author = "system"; // TODO: current user
        var nowUtc = DateTime.UtcNow;

        try
        {
            await CreateEntryHandler.HandleAsync(new TransitionTimesheetStateCommand(
                PersonId: dto.PersonId,
                Lane: dto.Lane,
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

    private static string GetSign(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => "ШТ",
            EnrollmentKind.AttachedByList => "НК",
            EnrollmentKind.AttachedByOrder => "БР",
            _ => "ВИКЛ"
        };
}
