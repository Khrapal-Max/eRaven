//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;

namespace eRaven.Application.Handlers.Personal;

/// <summary>
/// Command handler: переводить особу в життєвий цикл "В табелі" та відкриває (за потреби) епізод табеля.
/// </summary>
public sealed class EnrollCommandHandler(
    IPersonRepository repo,
    ITimesheetEpisodeRepository timesheetRepo)
    : ICommandHandler<EnrollCommand>
{
    private readonly IPersonRepository _repo = repo;
    private readonly ITimesheetEpisodeRepository _timesheet = timesheetRepo;

    /// <inheritdoc />
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

        // 2) Timesheet lifecycle: відкриваємо епізод + ставимо дефолтний main-код з дати зарахування
        await _timesheet.OpenOnEnrollAsync(
            personId: command.PersonId,
            enrollDate: command.EnrollDate,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
