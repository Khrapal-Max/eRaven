//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetStatusKindByIdQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries.StatusKinds;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.QueryHandlers.StatusKinds;

public sealed class GetStatusKindByIdQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
        : IQueryHandler<GetStatusKindByIdQuery, StatusKindDto?>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<StatusKindDto?> HandleAsync(
        GetStatusKindByIdQuery query,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var statusKind = await db.StatusKinds
            .AsNoTracking()
            .Where(s => s.Id == query.StatusKindId)
            .Select(s => new StatusKindDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Order = s.Order,
                IsActive = s.IsActive,
                Modified = s.Modified
            })
            .FirstOrDefaultAsync(ct);

        return statusKind;
    }
}
