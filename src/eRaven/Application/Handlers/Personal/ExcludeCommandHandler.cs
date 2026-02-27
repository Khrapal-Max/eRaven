//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;

namespace eRaven.Application.Handlers.Personal;

/// <summary>
/// Command handler: переводить особу в життєвий цикл "Резерв" та закриває активний епізод табеля.
/// </summary>
public sealed class ExcludeCommandHandler(
    IPersonRepository repo,
    ITimesheetEpisodeRepository timesheetRepo)
    : ICommandHandler<ExcludeCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetEpisodeRepository _timesheet = timesheetRepo;

    /// <inheritdoc />
    public async Task HandleAsync(ExcludeCommand command, CancellationToken ct = default)
    {
        // 0) Валідація ДО виключення: на дату EffectiveDate main має бути дозволений до закриття.
        await _timesheet.ValidateCanCloseOnExcludeAsync(
            personId: command.PersonId,
            closeTo: command.EffectiveDate,
            ct: ct);

        // 1) Person lifecycle
        await _repo.ExcludeAsync(
            command.PersonId,
            command.Reason,
            command.EffectiveDate,
            command.Author,
            command.NowUtc,
            ct);

        // 2) Timesheet lifecycle: закриваємо епізод, підрізаємо записи, видаляємо майбутні
        await _timesheet.CloseOnExcludeAsync(
            personId: command.PersonId,
            closeTo: command.EffectiveDate,
            reason: command.Reason,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
