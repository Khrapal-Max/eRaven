//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ApprovePlanActionCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PlanActions;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.CommandHandlers.PlanActions;

public sealed class ApprovePlanActionCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
        : ICommandHandler<ApprovePlanActionCommand>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task HandleAsync(
        ApprovePlanActionCommand cmd,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var action = await db.PlanActions
            .FirstOrDefaultAsync(a => a.Id == cmd.ActionId, ct)
            ?? throw new InvalidOperationException("Планова дія не знайдена");

        if (action.ActionState != ActionState.PlanAction)
            throw new InvalidOperationException("Дія вже затверджена");

        action.ActionState = ActionState.ApprovedOrder;
        action.Order = cmd.Order.Trim();

        await db.SaveChangesAsync(ct);
    }
}