//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Personal;

public sealed class ExcludeCommandHandler(
    IPersonRepository repo,
    ITimesheetRepository timesheetRepo)
    : ICommandHandler<ExcludeCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetRepository _timesheetRepo = timesheetRepo;

    public async Task HandleAsync(ExcludeCommand command, CancellationToken ct = default)
    {
        // 1) доменний факт: виключили з табелю (в PersonReadModel і т.д.)
        await _repo.ExcludeAsync(command, ct);

        // 2) “закрити табель” у CRUD-табелі:
        //    - усі записи, що виходять за EffectiveDate, обрізати до EffectiveDate
        //    - майбутні записи (From > EffectiveDate) — прибрати (soft delete), щоб не висіли
        await _timesheetRepo.EnsureClosedOnExcludeAsync(
            personId: command.PersonId,
            closeTo: command.EffectiveDate,
            reason: command.Reason,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
