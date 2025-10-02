//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetAllPositionsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries.Positions;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.QueryHandlers.Positions;

public sealed class GetAllPositionsQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
        : IQueryHandler<GetAllPositionsQuery, IReadOnlyList<PositionDto>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<PositionDto>> HandleAsync(
        GetAllPositionsQuery query,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.PositionUnits.AsNoTracking();

        if (query.OnlyActive)
            q = q.Where(p => p.IsActived);

        var positions = await q
            .OrderBy(p => p.Code ?? string.Empty)
            .ThenBy(p => p.ShortName)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Code = p.Code,
                ShortName = p.ShortName,
                OrgPath = p.OrgPath,
                SpecialNumber = p.SpecialNumber,
                FullName = string.IsNullOrWhiteSpace(p.OrgPath)
                    ? p.ShortName
                    : p.ShortName + " " + p.OrgPath,
                IsActived = p.IsActived,
                CurrentPersonFullName = p.FullName //TODO: lazy load
            })
            .ToListAsync(ct);

        return positions;
    }
}
