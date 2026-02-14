//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Personal;

public sealed class EnrollCommandHandler(
    IPersonRepository repo,
    ITimesheetLifecycleRepository timesheetRepo)
    : ICommandHandler<EnrollCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetLifecycleRepository _timesheet = timesheetRepo;

    public async Task HandleAsync(EnrollCommand command, CancellationToken ct = default)
    {
        // 1) Person lifecycle
        await _repo.EnrollAsync(
            command.PersonId,
            command.Kind,
            command.Reference,
            command.Reason,
            command.EnrollDate,
            command.Rank,
            command.PositionSort,
            command.Position,
            command.Author,
            command.NowUtc,
            ct);

        // 2) Timesheet lifecycle: відкриваємо шкали + ставимо Main=Т
        await _timesheet.OpenOnEnrollAsync(
            personId: command.PersonId,
            enrollDate: command.EnrollDate,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
