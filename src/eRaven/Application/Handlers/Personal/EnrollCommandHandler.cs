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
    ITimesheetRepository timesheet)
    : ICommandHandler<EnrollCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetRepository _timesheet = timesheet;

    public async Task HandleAsync(EnrollCommand command, CancellationToken ct = default)
    {
        // 1) зарахували картку (PersonRead / events)
        await _repo.EnrollAsync(command, ct);

        // 2) “відкрили табель” (мінімально: дефолт Main=30 з дати зарахування)
        await _timesheet.EnsureOpenedOnEnrollAsync(
            personId: command.PersonId,
            enrollDate: command.EnrollDate,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
