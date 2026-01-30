//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdateCombatTaskGroupCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class UpdateCombatTaskGroupCommandHandler(
    ICombatTaskEntryRepository repo)
    : ICommandHandler<UpdateCombatTaskGroupCommand>
{
    public async Task HandleAsync(UpdateCombatTaskGroupCommand cmd, CancellationToken ct = default)
        => await repo.UpdateGroupAsync(
            cmd.DocumentId,
            cmd.GroupId,
            cmd.SourceDocNo,
            cmd.Action,
            cmd.MissionId,
            cmd.MissionDisplaySnapshot,
            cmd.ActionDate,
            cmd.Author,
            cmd.NowUtc,
            ct);
}