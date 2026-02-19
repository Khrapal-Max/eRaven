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
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.DTOs.Person;
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

        // 2) Load document to detect existing mission block and to use as fallback for snapshot.
        var document = await _repo.GetDocumentAsync(command.DocumentId, ct);

        var missionEntity = document.CombatTasks.FirstOrDefault(x => x.MissionId == command.MissionId);

        CombatTaskMissionBlockDto? existingBlock = null;

        if (missionEntity is not null)
        {
            existingBlock = new CombatTaskMissionBlockDto(
                CombatTaskId: missionEntity.Id,
                MissionId: missionEntity.MissionId,
                MissionName: missionEntity.Mission?.ToString() ?? string.Empty,
                SourceDocument: missionEntity.SourceDocument ?? string.Empty,
                CombatTaskDetails: [.. missionEntity.CombatTaskDetails
                    .OrderBy(l => l.EffectiveAt)
                    .ThenBy(l => l.Kind)
                    .ThenBy(l => l.FullName)
                    .ThenBy(l => l.Id)
                    .Select(l => new CombatTaskDetailsDto(
                        CombatTaskDetailsId: l.Id,
                        Kind: l.Kind,
                        EffectiveAt: l.EffectiveAt,
                        PersonId: l.PersonId,
                        Rnokpp: l.Rnokpp,
                        Rank: l.Rank,
                        FullName: l.FullName,
                        Position: l.Position,
                        Weapon: l.Weapon,
                        Callsign: l.Callsign
                    ))]
            );
        }

        // 2.1) Enrich missing snapshot fields: existing rows -> PersonRead.
        await EnrichSnapshotAsync(incoming, existingBlock, ct);

        // 2.2) Final validation after enrichment (guard against persisting empty required snapshot).
        EnsureSnapshotIsComplete(incoming);

        Guid combatTaskId;
        IReadOnlyCollection<CombatTaskDetails> persistedRows;

        // NOTE: de-dup also for Create (protect against double rows with same business key)
        if (existingBlock is null)
        {
            var normalized = MergeByBusinessKey(existing: [], incoming: incoming);
            persistedRows = normalized;

            combatTaskId = await _repo.CreateCombatTaskAsync(
                documentId: command.DocumentId,
                missionId: command.MissionId,
                sourceDocument: command.SourceDocument.Trim(),
                combatTaskDetails: normalized,
                ct: ct);
        }
        else
        {
            var existing = new List<CombatTaskDetails>(existingBlock.CombatTaskDetails.Count);

            foreach (var x in existingBlock.CombatTaskDetails)
            {
                existing.Add(new CombatTaskDetails
                {
                    Id = x.CombatTaskDetailsId,
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

            var merged = MergeByBusinessKey(existing, incoming);
            persistedRows = merged;

            combatTaskId = await _repo.UpsertCombatTaskAsync(
                documentId: command.DocumentId,
                missionId: command.MissionId,
                sourceDocument: command.SourceDocument.Trim(),
                combatTaskDetails: merged,
                ct: ct);
        }

        // 3) Timesheet: apply facts with the SAME rows that were persisted (incoming or merged).
        await _timesheets.ApplyCombatTaskFactsAsync(
            documentId: command.DocumentId,
            missionId: command.MissionId,
            details: persistedRows,
            author: command.Author.Trim(),
            nowUtc: command.NowUtc,
            ct: ct);

        return combatTaskId;
    }

    /// <summary>
    /// Підтягує відсутні snapshot-поля для incoming:
    /// <list type="number">
    /// <item><description>спочатку з існуючих рядків місії в документі (якщо місія вже створена);</description></item>
    /// <item><description>потім — з PersonRead (через <see cref="IPersonRepository"/>), якщо все ще пусто.</description></item>
    /// </list>
    /// </summary>
    private async Task EnrichSnapshotAsync(
        List<CombatTaskDetails> incoming,
        CombatTaskMissionBlockDto? existingBlock,
        CancellationToken ct)
    {
        // 1) Fallback з existing rows (якщо є)
        var fallback = new Dictionary<Guid, CombatTaskDetailsDto>();

        if (existingBlock is not null)
        {
            foreach (var g in existingBlock.CombatTaskDetails.GroupBy(x => x.PersonId))
            {
                // Пріоритет: Start -> End -> перший
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

        // 2) Якщо все ще є “дірки” — добираємо з PersonRead через IPersonRepository
        var need = incoming
            .Where(IsMissingSnapshot)
            .Select(x => x.PersonId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (need.Count == 0)
            return;

        foreach (var personId in need)
        {
            PersonDetailsDto? p = await _persons.GetByIdAsync(personId, ct);
            if (p is null)
                continue;

            var rank = TrimOrNull(p.Rank);
            var position = TrimOrNull(p.Position);
            var weapon = TrimOrNull(p.Weapon);
            var callsign = TrimOrNull(p.Callsign);

            foreach (var row in incoming.Where(x => x.PersonId == personId))
            {
                row.Rank ??= rank;
                row.Position ??= position;
                row.Weapon ??= weapon;
                row.Callsign ??= callsign;

                if (string.IsNullOrWhiteSpace(row.Rnokpp))
                    row.Rnokpp = (p.Rnokpp ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(row.FullName))
                    row.FullName = (p.FullName ?? string.Empty).Trim();
            }
        }
    }

    /// <summary>
    /// Зливає snapshot-рядки по бізнес-ключу (PersonId + Kind + EffectiveAt).
    /// Якщо incoming має той самий ключ — оновлюємо snapshot, але зберігаємо Id існуючого рядка.
    /// </summary>
    private static IReadOnlyCollection<CombatTaskDetails> MergeByBusinessKey(
        IReadOnlyCollection<CombatTaskDetails> existing,
        IReadOnlyCollection<CombatTaskDetails> incoming)
    {
        static string Key(CombatTaskDetails d)
            => $"{d.PersonId:N}|{(int)d.Kind}|{d.EffectiveAt:yyyy-MM-dd}";

        var map = new Dictionary<string, CombatTaskDetails>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in existing)
            map[Key(e)] = e;

        foreach (var n in incoming)
        {
            var k = Key(n);

            if (!map.TryGetValue(k, out var e))
            {
                map[k] = n;
                continue;
            }

            // incoming "перемагає" по snapshot, але Id лишаємо існуючий
            e.Rnokpp = n.Rnokpp;
            e.FullName = n.FullName;
            e.Rank = n.Rank;
            e.Position = n.Position;
            e.Weapon = n.Weapon;
            e.Callsign = n.Callsign;
        }

        return [.. map.Values
            .OrderBy(x => x.EffectiveAt)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.Id)];
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
           || string.IsNullOrWhiteSpace(x.FullName)
           || x.Rank is null
           || x.Position is null
           || x.Weapon is null
           || x.Callsign is null;
}
