//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DeleteCombatTaskGroupCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class DeleteCombatTaskGroupCommandHandler(
    ICombatTaskEntryRepository entries)
    : ICommandHandler<DeleteCombatTaskGroupCommand>
{
    public async Task HandleAsync(DeleteCombatTaskGroupCommand cmd, CancellationToken ct = default)
        => await entries.DeleteGroupAsync(cmd.DocumentId, cmd.GroupId, ct);
}