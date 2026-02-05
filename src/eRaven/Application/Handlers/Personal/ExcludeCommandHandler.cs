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
    ITimesheetLifecycleRepository timesheetRepo)
    : ICommandHandler<ExcludeCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetLifecycleRepository _timesheet = timesheetRepo;

    public async Task HandleAsync(ExcludeCommand command, CancellationToken ct = default)
    {
        // 0) Валідація ДО виключення: на дату EffectiveDate Main має бути Т або РОЗПОР
        // (щоб не було кейсу "звільнили у відпустці")
        await _timesheet.ValidateCanCloseOnExcludeAsync(
            personId: command.PersonId,
            closeTo: command.EffectiveDate,
            ct: ct);

        // 1) Person lifecycle
        await _repo.ExcludeAsync(command, ct);

        // 2) Timesheet lifecycle: закриваємо шкали, обрізаємо записи, видаляємо майбутні
        await _timesheet.CloseOnExcludeAsync(
            personId: command.PersonId,
            closeTo: command.EffectiveDate,
            reason: command.Reason,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
