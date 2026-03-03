//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskDocumentsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Повертає документи з завданнями.
/// </summary>

public sealed class GetCombatTaskDocumentsQueryHandler(ICombatTaskDocumentRepository repo)
    : IQueryHandler<GetCombatTaskDocumentsQuery, IReadOnlyList<CombatTaskDocumentDto>>
{
    private readonly ICombatTaskDocumentRepository _repo = repo;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CombatTaskDocumentDto>> HandleAsync(GetCombatTaskDocumentsQuery query, CancellationToken ct = default)
    {
        var s = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var status = CombatTaskEnumMapper.ToDomainStatus(query.Status);
        var task = await _repo.GetDocumentsAsync(query.Year, query.Month, status, s, ct);

        return [.. task.Select(x => new CombatTaskDocumentDto(
                 DocumentId: x.Id,
                 OrderTitle: x.OrderTitle,
                 Description: x.Description,
                 Status: CombatTaskEnumMapper.MapStatus(x.Status),
                 RecordedAt: x.RecordedAt,
                 CanceledReason: x.CanceledReason ?? string.Empty))];
    }
}