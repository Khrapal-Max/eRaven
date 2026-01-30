//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddCombatTaskGroupCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class AddCombatTaskGroupCommandHandler(
    ICombatTaskEntryRepository entries)
    : ICommandHandler<AddCombatTaskGroupCommand, Guid>
{
    public async Task<Guid> HandleAsync(AddCombatTaskGroupCommand cmd, CancellationToken ct = default)
        => await entries.AddGroupAsync(
            cmd.DocumentId,
            cmd.SourceDocNo,
            cmd.Action,
            cmd.MissionId,
            cmd.MissionDisplaySnapshot,
            cmd.ActionDate,
            cmd.PersonIds,
            cmd.Author,
            cmd.NowUtc,
            ct);
}