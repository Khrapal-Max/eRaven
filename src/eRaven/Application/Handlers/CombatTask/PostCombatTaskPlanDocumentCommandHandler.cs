//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PostCombatTaskPlanDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class PostCombatTaskPlanDocumentCommandHandler(
    ICombatTaskPlanDocumentRepository repo)
    : ICommandHandler<PostCombatTaskPlanDocumentCommand>
{
    private readonly ICombatTaskPlanDocumentRepository _repo = repo;

    public async Task HandleAsync(PostCombatTaskPlanDocumentCommand command, CancellationToken ct = default)
        => await _repo.PostAsync(command.DocumentId, command.Author, command.NowUtc, ct);
}