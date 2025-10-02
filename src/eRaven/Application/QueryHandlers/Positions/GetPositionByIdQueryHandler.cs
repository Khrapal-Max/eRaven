//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPositionByIdQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries.Positions;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.QueryHandlers.Positions;

public sealed class GetPositionByIdQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
        : IQueryHandler<GetPositionByIdQuery, PositionDto?>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<PositionDto?> HandleAsync(
        GetPositionByIdQuery query,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var position = await db.PositionUnits
            .AsNoTracking()
            .Where(p => p.Id == query.PositionId)
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
                IsActived = p.IsActived
            })
            .FirstOrDefaultAsync(ct);

        return position;
    }
}