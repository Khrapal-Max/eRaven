//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.Positions;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.Positions;

public sealed class CreatePositionCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
        : ICommandHandler<CreatePositionCommand, Guid>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<Guid> HandleAsync(
        CreatePositionCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Перевірка дублю коду серед активних
        if (!string.IsNullOrWhiteSpace(cmd.Code))
        {
            var exists = await db.PositionUnits
                .AnyAsync(p => p.IsActived && p.Code == cmd.Code.Trim(), ct);

            if (exists)
                throw new InvalidOperationException("Активна посада з таким кодом вже існує");
        }

        var position = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = string.IsNullOrWhiteSpace(cmd.Code) ? null : cmd.Code.Trim(),
            ShortName = cmd.ShortName.Trim(),
            SpecialNumber = cmd.SpecialNumber.Trim(),
            OrgPath = cmd.OrgPath.Trim(),
            IsActived = true
        };

        db.PositionUnits.Add(position);
        await db.SaveChangesAsync(ct);

        return position.Id;
    }
}