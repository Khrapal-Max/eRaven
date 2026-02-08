//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class CreateCombatTaskCommandHandler(ICombatTaskRepository repo)
    : ICommandHandler<CreateCombatTaskCommand, Guid>
{
    private readonly ICombatTaskRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreateCombatTaskCommand command, CancellationToken ct = default)
    {
        var details = command.CombatTaskDetails
            .Select(x => new CombatTaskDetails
            {
                Id = x.CombatTaskDetailsId,      // ✅ це Id рядка
                CombatTaskId = Guid.Empty,       // ✅ репо проставить правильний FK
                CombatTask = null,

                Kind = x.Kind,
                EffectiveAt = x.EffectiveAt,
                PersonId = x.PersonId,

                Rnokpp = x.Rnokpp,
                FullName = x.FullName,
                Callsign = x.Callsign            // ✅ не форсимо ""
            })
            .ToList();

        return await _repo.CreateCombatTask(
            documentId: command.DocumentId,
            missionId: command.MissionId,
            sourceDocument: command.SourceDocument,
            combatTaskDetails: details,
            ct: ct);
    }
}