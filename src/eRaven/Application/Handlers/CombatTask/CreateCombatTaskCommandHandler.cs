//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class CreateCombatTaskCommandHandler(
    ICombatTaskRepository repo,
    IMissionAssignmentRepository assignments)
    : ICommandHandler<CreateCombatTaskCommand, Guid>
{
    private readonly ICombatTaskRepository _repo = repo;
    private readonly IMissionAssignmentRepository _assignments = assignments;

    public async Task<Guid> HandleAsync(CreateCombatTaskCommand command, CancellationToken ct = default)
    {
        var details = command.CombatTaskDetails
            .Select(x => new CombatTaskDetails
            {
                Id = x.CombatTaskDetailsId,
                CombatTaskId = Guid.Empty,
                CombatTask = null,
                Kind = x.Kind,
                EffectiveAt = x.EffectiveAt,
                PersonId = x.PersonId,
                Rnokpp = x.Rnokpp,
                FullName = x.FullName,
                Callsign = x.Callsign
            })
            .ToList();

        var combatTaskId = await _repo.CreateCombatTask(
            documentId: command.DocumentId,
            missionId: command.MissionId,
            sourceDocument: command.SourceDocument,
            combatTaskDetails: details,
            ct: ct);

        // Draft = план → пишемо Planned проєкцію
        var lines = command.CombatTaskDetails
            .Select(x => new ApplyCombatTaskDetailsDto(
                DocumentId: command.DocumentId,
                CombatTaskId: combatTaskId,
                DetailsId: x.CombatTaskDetailsId,
                MissionId: command.MissionId,
                PersonId: x.PersonId,
                Kind: x.Kind,
                EffectiveAt: x.EffectiveAt))
            .ToList();

        await _assignments.ApplyDraftLinesAsync(lines, ct);

        return combatTaskId;
    }
}
