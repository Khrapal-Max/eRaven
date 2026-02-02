//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ReplaceCombatTaskGroupPersonsCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class ReplaceCombatTaskGroupPersonsCommandHandler(
    ICombatTaskEntryRepository entries)
    : ICommandHandler<ReplaceCombatTaskGroupPersonsCommand>
{
    public async Task HandleAsync(ReplaceCombatTaskGroupPersonsCommand cmd, CancellationToken ct = default)
        => await entries.ReplaceGroupPersonsAsync(
            cmd.DocumentId,
            cmd.GroupId,
            cmd.PersonIds,
            cmd.Author,
            cmd.NowUtc,
            ct);
}