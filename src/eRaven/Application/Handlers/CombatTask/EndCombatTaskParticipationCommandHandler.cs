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

/// <summary>
/// Закриття активних участей.
/// </summary>
public sealed class EndCombatTaskGroupCommandHandler(
    IMissionParticipationRepository repo)
    : ICommandHandler<EndCombatTaskGroupCommand>
{
    private readonly IMissionParticipationRepository _repo = repo;

    public async Task HandleAsync(EndCombatTaskGroupCommand cmd, CancellationToken ct = default)
    {
        if (cmd.DocumentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(cmd));
        if (cmd.GroupId == Guid.Empty)
            throw new ArgumentException("GroupId is required.", nameof(cmd));
        if (string.IsNullOrWhiteSpace(cmd.EndSourceDocNo))
            throw new ArgumentException("EndSourceDocNo is required.", nameof(cmd));

        await _repo.EndGroupAsync(
              documentId: cmd.DocumentId,
              groupId: cmd.GroupId,
              to: cmd.To,
              endSourceDocNo: cmd.EndSourceDocNo,
              author: cmd.Author,
              nowUtc: cmd.NowUtc,
              ct: ct);
    }
}
