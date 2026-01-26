//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CancelCombatTaskPlanDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class CancelCombatTaskPlanDocumentCommandHandler(
    ICombatTaskPlanDocumentRepository repo)
    : ICommandHandler<CancelCombatTaskPlanDocumentCommand>
{
    private readonly ICombatTaskPlanDocumentRepository _repo = repo;

    public async Task HandleAsync(CancelCombatTaskPlanDocumentCommand command, CancellationToken ct = default)
        => await _repo.CancelAsync(command.DocumentId, command.Reason, command.Author, command.NowUtc, ct);
}
