//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentDraftCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class CreateCombatTaskDocumentDraftCommandHandler(
    ICombatTaskDocumentRepository repo)
    : ICommandHandler<CreateCombatTaskDocumentDraftCommand, Guid>
{
    private readonly ICombatTaskDocumentRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreateCombatTaskDocumentDraftCommand command, CancellationToken ct = default)
        => await _repo.CreateDraftAsync(
            orderTitle: command.OrderTitle,
            description: command.Description,
            recordedAt: command.RecordedAt,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
}