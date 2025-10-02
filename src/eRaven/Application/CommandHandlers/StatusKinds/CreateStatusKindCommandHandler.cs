//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateStatusKindCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.StatusKinds;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.StatusKinds;

public sealed class CreateStatusKindCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
        : ICommandHandler<CreateStatusKindCommand, int>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<int> HandleAsync(
        CreateStatusKindCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var statusKind = new StatusKind
        {
            Name = cmd.Name.Trim(),
            Code = cmd.Code.Trim(),
            Order = cmd.Order,
            IsActive = cmd.IsActive,
            Author = "ui",
            Modified = DateTime.UtcNow
        };

        db.StatusKinds.Add(statusKind);
        await db.SaveChangesAsync(ct);

        return statusKind.Id;
    }
}
