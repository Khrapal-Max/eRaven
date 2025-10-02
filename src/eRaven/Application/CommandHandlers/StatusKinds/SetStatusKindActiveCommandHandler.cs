//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetStatusKindActiveCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.StatusKinds;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.StatusKinds;

public sealed class SetStatusKindActiveCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
        : ICommandHandler<SetStatusKindActiveCommand>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task HandleAsync(
        SetStatusKindActiveCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var statusKind = await db.StatusKinds
            .FirstOrDefaultAsync(s => s.Id == cmd.StatusKindId, ct)
            ?? throw new InvalidOperationException("Статус не знайдено");

        statusKind.IsActive = cmd.IsActive;
        statusKind.Modified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
