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

/// <summary>
/// Handler старту групи участей (створення інтервалів MissionParticipation).
/// </summary>
public sealed class StartCombatTaskGroupCommandHandler(
    IMissionParticipationRepository repo)
    : ICommandHandler<StartCombatTaskGroupCommand, Guid>
{
    private readonly IMissionParticipationRepository _repo = repo;

    public async Task<Guid> HandleAsync(StartCombatTaskGroupCommand cmd, CancellationToken ct = default)
    {
        if (cmd is null) throw new ArgumentNullException(nameof(cmd));

        if (cmd.DocumentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(cmd)); // <- без CS2208

        if (cmd.MissionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(cmd));

        if (string.IsNullOrWhiteSpace(cmd.SourceDocNo))
            throw new ArgumentException("SourceDocNo is required.", nameof(cmd));

        if (cmd.Persons is null || cmd.Persons.Count == 0)
            throw new ArgumentException("Persons is required.", nameof(cmd));

        // Нормалізуємо список: distinct by PersonId, без пустих
        var persons = cmd.Persons
            .Where(p => p.PersonId != Guid.Empty)
            .GroupBy(p => p.PersonId)
            .Select(g => g.First())
            .ToList();

        if (persons.Count == 0)
            throw new ArgumentException("Persons list is empty.", nameof(cmd));

        return await _repo.StartGroupAsync(
            documentId: cmd.DocumentId,
            sourceDocNo: cmd.SourceDocNo,
            missionId: cmd.MissionId,
            missionDisplaySnapshot: cmd.MissionDisplaySnapshot,
            from: cmd.From,
            persons: persons,
            author: cmd.Author,
            nowUtc: cmd.NowUtc,
            ct: ct);
    }
}