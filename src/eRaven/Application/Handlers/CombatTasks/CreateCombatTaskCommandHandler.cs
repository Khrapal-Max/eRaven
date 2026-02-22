//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Створює/оновлює контент місії у документі бойових завдань
/// та застосовує факти у табель (TaskSpans).
/// </summary>
public sealed class CreateCombatTaskCommandHandler(
    ICombatTaskRepository repo,
    ITimesheetAggregateRepository timesheets,
    IPersonRepository persons)
    : ICommandHandler<CreateCombatTaskCommand, Guid>
{
    private readonly ICombatTaskRepository _repo = repo;
    private readonly ITimesheetAggregateRepository _timesheets = timesheets;
    private readonly IPersonRepository _persons = persons;

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(CreateCombatTaskCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.DocumentId == Guid.Empty)
            throw new InvalidOperationException("DocumentId обов'язковий.");

        if (command.MissionId == Guid.Empty)
            throw new InvalidOperationException("MissionId обов'язковий.");

        if (string.IsNullOrWhiteSpace(command.SourceDocument))
            throw new InvalidOperationException("SourceDocument обов'язковий.");

        if (string.IsNullOrWhiteSpace(command.Author))
            throw new InvalidOperationException("Author обов'язковий.");

        if (command.NowUtc == default)
            throw new InvalidOperationException("NowUtc обов'язковий.");

        // 1) Map incoming DTO -> domain snapshot rows.
        // Runtime safety: CombatTaskDetails може бути null (незалежно від nullable-анотацій).
        var dtoRows = command.CombatTaskDetails ?? [];

        if (dtoRows.Count == 0)
            throw new InvalidOperationException("Порожній список рядків завдання.");

        var incoming = new List<CombatTaskDetails>(dtoRows.Count);

        foreach (var x in dtoRows)
        {
            if (x is null)
                throw new InvalidOperationException("CombatTaskDetails містить null-рядок.");

            if (x.PersonId == Guid.Empty)
                throw new InvalidOperationException("PersonId обов'язковий для кожного рядка.");

            if (x.EffectiveAt == default)
                throw new InvalidOperationException("EffectiveAt обов'язковий для кожного рядка.");

            incoming.Add(new CombatTaskDetails
            {
                Id = x.CombatTaskDetailsId == Guid.Empty ? Guid.NewGuid() : x.CombatTaskDetailsId,
                CombatTaskId = Guid.Empty,
                CombatTask = null,

                Kind = x.Kind,
                EffectiveAt = x.EffectiveAt,
                PersonId = x.PersonId,

                Rnokpp = (x.Rnokpp ?? string.Empty).Trim(),
                FullName = (x.FullName ?? string.Empty).Trim(),

                Rank = TrimOrNull(x.Rank),
                Position = TrimOrNull(x.Position),
                Weapon = TrimOrNull(x.Weapon),
                Callsign = TrimOrNull(x.Callsign)
            });
        }

        // 2) Load document + find mission entity (if exists)
        var document = await _repo.GetDocumentAsync(command.DocumentId, ct);
        var missionEntity = document.CombatTasks.FirstOrDefault(x => x.MissionId == command.MissionId);

        // 2.1) Enrich snapshot from existing mission rows (entity) and then PersonRead
        await EnrichSnapshotAsync(incoming, missionEntity, ct);

        EnsureSnapshotIsComplete(incoming);

        Guid combatTaskId;

        if (missionEntity is null)
        {
            combatTaskId = await _repo.CreateCombatTaskAsync(
                documentId: command.DocumentId,
                missionId: command.MissionId,
                sourceDocument: command.SourceDocument.Trim(),
                combatTaskDetails: incoming,
                ct: ct);
        }
        else
        {
            // Replace-all: without merge
            combatTaskId = await _repo.UpsertCombatTaskAsync(
                documentId: command.DocumentId,
                missionId: command.MissionId,
                sourceDocument: command.SourceDocument.Trim(),
                combatTaskDetails: incoming,
                ct: ct);
        }

        // 3) Apply facts to timesheet using the same "current truth"
        await _timesheets.ApplyCombatTaskFactsAsync(
            documentId: command.DocumentId,
            missionId: command.MissionId,
            documentOrderTitle: document.OrderTitle,
            details: incoming,
            author: command.Author.Trim(),
            nowUtc: command.NowUtc,
            ct: ct);

        return combatTaskId;
    }

    /// <summary>
    /// Підтягує відсутні snapshot-поля для incoming:
    /// <list type="number">
    /// <item><description>спочатку з існуючих рядків місії в документі;</description></item>
    /// <item><description>потім — з PersonRead (через <see cref="IPersonRepository"/>).</description></item>
    /// </list>
    /// </summary>
    private async Task EnrichSnapshotAsync(
        List<CombatTaskDetails> incoming,
        CombatTask? missionEntity,
        CancellationToken ct)
    {
        // 1) Fallback з existing rows (entity)
        var fallback = new Dictionary<Guid, CombatTaskDetails>();

        if (missionEntity is not null)
        {
            foreach (var g in missionEntity.CombatTaskDetails.GroupBy(x => x.PersonId))
            {
                var best = g.FirstOrDefault(x => x.Kind == CombatTaskDetailsKind.Start)
                           ?? g.FirstOrDefault(x => x.Kind == CombatTaskDetailsKind.End)
                           ?? g.First();

                fallback[g.Key] = best;
            }
        }

        foreach (var row in incoming)
        {
            if (!fallback.TryGetValue(row.PersonId, out var fb))
                continue;

            row.Rank ??= TrimOrNull(fb.Rank);
            row.Position ??= TrimOrNull(fb.Position);
            row.Weapon ??= TrimOrNull(fb.Weapon);
            row.Callsign ??= TrimOrNull(fb.Callsign);

            if (string.IsNullOrWhiteSpace(row.Rnokpp))
                row.Rnokpp = (fb.Rnokpp ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(row.FullName))
                row.FullName = (fb.FullName ?? string.Empty).Trim();
        }

        // 2) Добір з PersonRead
        var need = incoming
            .Where(IsMissingSnapshot)
            .Select(x => x.PersonId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var personId in need)
        {
            var person = await _persons.GetByIdAsync(personId, ct);
            if (person is null) continue;

            var rank = TrimOrNull(person.Rank);
            var position = TrimOrNull(person.Position);
            var weapon = TrimOrNull(person.Weapon);
            var callsign = TrimOrNull(person.Callsign);

            foreach (var row in incoming.Where(x => x.PersonId == personId))
            {
                row.Rank ??= rank;
                row.Position ??= position;
                row.Weapon ??= weapon;
                row.Callsign ??= callsign;

                if (string.IsNullOrWhiteSpace(row.Rnokpp))
                    row.Rnokpp = (person.Rnokpp ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(row.FullName))
                    row.FullName = (person.FullName ?? string.Empty).Trim();
            }
        }
    }

    /// <summary>
    /// Гарантує, що після enrichment ми не пишемо "порожні" snapshot-рядки.
    /// </summary>
    private static void EnsureSnapshotIsComplete(IReadOnlyCollection<CombatTaskDetails> rows)
    {
        foreach (var r in rows)
        {
            if (r.PersonId == Guid.Empty)
                throw new InvalidOperationException("PersonId обов'язковий для кожного рядка.");

            if (r.EffectiveAt == default)
                throw new InvalidOperationException("EffectiveAt обов'язковий для кожного рядка.");

            if (string.IsNullOrWhiteSpace(r.Rnokpp))
                throw new InvalidOperationException("Rnokpp обов'язковий для кожного рядка (після enrichment).");

            if (string.IsNullOrWhiteSpace(r.FullName))
                throw new InvalidOperationException("FullName обов'язковий для кожного рядка (після enrichment).");
        }
    }

    private static string? TrimOrNull(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static bool IsMissingSnapshot(CombatTaskDetails x)
        => string.IsNullOrWhiteSpace(x.Rnokpp)
           || string.IsNullOrWhiteSpace(x.FullName);
}
