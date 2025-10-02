//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DeletePlanActionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PlanActions;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.PlanActions;

public sealed class DeletePlanActionCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
        : ICommandHandler<DeletePlanActionCommand>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task HandleAsync(
        DeletePlanActionCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var deleted = await db.PlanActions
            .Where(a => a.Id == cmd.ActionId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new InvalidOperationException("Планова дія не знайдена");
    }
}