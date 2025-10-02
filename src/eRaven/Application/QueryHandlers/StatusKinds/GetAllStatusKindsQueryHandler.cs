//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetAllStatusKindsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries.StatusKinds;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.QueryHandlers.StatusKinds;

public sealed class GetAllStatusKindsQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
        : IQueryHandler<GetAllStatusKindsQuery, IReadOnlyList<StatusKindDto>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<StatusKindDto>> HandleAsync(
        GetAllStatusKindsQuery query,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.StatusKinds.AsNoTracking();

        if (!query.IncludeInactive)
            q = q.Where(s => s.IsActive);

        var statusKinds = await q
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Name)
            .Select(s => new StatusKindDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Order = s.Order,
                IsActive = s.IsActive,
                Modified = s.Modified
            })
            .ToListAsync(ct);

        return statusKinds;
    }
}