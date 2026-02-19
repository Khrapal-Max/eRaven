//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDetailsByDocumentIdQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskDetailsByDocumentIdQueryHandler(ICombatTaskRepository repo)
    : IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?>
{
    private readonly ICombatTaskRepository _repo = repo;

    public async Task<CombatTaskEditorDto?> HandleAsync(GetCombatTaskDetailsByDocumentIdQuery query, CancellationToken ct = default)
    {
        var document = await _repo.GetDocumentAsync(query.DocumentId, ct);

        var tasks = document.CombatTasks
           .OrderBy(x => x.MissionId)
           .ThenBy(x => x.Id);

        var missions = tasks
            .Select(t => new CombatTaskMissionBlockDto(
                CombatTaskId: t.Id,
                MissionId: t.MissionId,
                MissionName: t.Mission?.ToString() ?? string.Empty,
                SourceDocument: t.SourceDocument ?? string.Empty,
                CombatTaskDetails: [.. t.CombatTaskDetails
                    .OrderBy(l => l.EffectiveAt)
                    .ThenBy(l => l.Kind)
                    .ThenBy(l => l.FullName)
                    .ThenBy(l => l.Id)
                    .Select(l => new CombatTaskDetailsDto(
                        CombatTaskDetailsId: l.Id,
                        Kind: l.Kind,
                        EffectiveAt: l.EffectiveAt,
                        PersonId: l.PersonId,
                        Rnokpp: l.Rnokpp,
                        Rank: l.Rank,
                        FullName: l.FullName,
                        Position: l.Position,
                        Weapon: l.Weapon,
                        Callsign: l.Callsign
                    ))]
            ))
            .ToList();

        return new CombatTaskEditorDto(
            DocumentId: document.Id,
            DocumentName: document.OrderTitle,
            Description: document.Description,
            Status: document.Status,
            RecordedAt: document.RecordedAt,
            Missions: missions);
    }
}