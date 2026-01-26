//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskPlanDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class CreateCombatTaskPlanDraftDocumentCommandHandler(
    ICombatTaskPlanDocumentRepository repo)
    : ICommandHandler<CreateCombatTaskPlanDraftDocumentCommand, Guid>
{
    private readonly ICombatTaskPlanDocumentRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreateCombatTaskPlanDraftDocumentCommand command, CancellationToken ct = default)
        => await _repo.CreateDraftAsync(
            recordedAt: command.RecordedAt,
            planningDate: command.PlanningDate,
            planningDocTitle: command.PlanningDocTitle,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
}