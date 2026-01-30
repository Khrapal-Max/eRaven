//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public class CombatTaskEntryRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskEntryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<Guid> AddGroupAsync(
       Guid documentId,
       string sourceDocNo,
       ActionKind action,
       Guid missionId,
       string missionDisplaySnapshot,
       DateOnly actionDate,
       IReadOnlyCollection<Guid> personIds,
       string author,
       DateTime nowUtc,
       CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments.FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var distinct = personIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinct.Length == 0) throw new ArgumentException("Потрібна хоча б одна особа.", nameof(personIds));

        // беремо “живі” дані особи (це НЕ бізнес-логіка, це просто snapshot)
        var persons = await db.PersonRead
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .Where(p => distinct.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Rnokpp,
                p.FullName,
                p.Rank,
                p.Position,
                p.Weapon,
                p.Callsign
            })
            .ToListAsync(ct);

        var found = persons.Select(x => x.Id).ToHashSet();
        var missing = distinct.Where(x => !found.Contains(x)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("Не знайдені особи: " + string.Join(", ", missing));

        // наступний group sequence
        var nextSeq = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId)
            .Select(x => (int?)x.GroupSequence)
            .MaxAsync(ct) ?? 0;

        var groupId = Guid.NewGuid();
        var groupSeq = nextSeq + 1;

        foreach (var p in persons)
        {
            db.CombatTaskEntries.Add(new CombatTaskEntry
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,

                GroupId = groupId,
                GroupSequence = groupSeq,

                SourceDocNo = sourceDocNo.Trim(),
                Action = action,
                ActionDate = actionDate,

                MissionId = missionId,
                MissionDisplaySnapshot = missionDisplaySnapshot,

                PersonId = p.Id,
                RNOKPP = p.Rnokpp ?? string.Empty,
                FullName = p.FullName ?? string.Empty,
                Rank = p.Rank ?? string.Empty,
                Position = p.Position ?? string.Empty,
                Weapon = p.Weapon ?? string.Empty,
                Callsign = p.Callsign ?? string.Empty
            });
        }

        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
        return groupId;
    }

    public async Task DeleteGroupAsync(Guid documentId, Guid groupId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var entries = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (entries.Count == 0)
            return; // ідемпотентність

        db.CombatTaskEntries.RemoveRange(entries);

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateGroupAsync(
        Guid documentId,
        Guid groupId,
        string sourceDocNo,
        ActionKind action,
        Guid missionId,
        string missionDisplaySnapshot,
        DateOnly actionDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));
        if (string.IsNullOrWhiteSpace(sourceDocNo)) throw new ArgumentException("SourceDocNo is required.", nameof(sourceDocNo));
        if (missionId == Guid.Empty) throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(missionDisplaySnapshot)) throw new ArgumentException("MissionDisplaySnapshot is required.", nameof(missionDisplaySnapshot));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var entries = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (entries.Count == 0)
            throw new InvalidOperationException("Групу не знайдено.");

        var src = sourceDocNo.Trim();

        foreach (var e in entries)
        {
            e.SourceDocNo = src;
            e.Action = action;
            e.ActionDate = actionDate;

            e.MissionId = missionId;
            e.MissionDisplaySnapshot = missionDisplaySnapshot;
        }

        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task ReplacePersonsInGroupAsync(
         Guid documentId,
         Guid groupId,
         IReadOnlyCollection<Guid> personIds,
         string author,
         DateTime nowUtc,
         CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));

        // normalize input (distinct, no empty)
        var desired = personIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        // беремо всі рядки групи (tracked), бо будемо видаляти/додавати
        var existingRows = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (existingRows.Count == 0)
            throw new InvalidOperationException("Групу не знайдено.");

        // якщо бажаний список порожній -> видаляємо всю групу
        if (desired.Length == 0)
        {
            db.CombatTaskEntries.RemoveRange(existingRows);

            doc.UpdatedBy = author;
            doc.UpdatedAtUtc = nowUtc;

            await db.SaveChangesAsync(ct);
            return;
        }

        // "шапка" групи (метадані однакові для всіх рядків групи)
        var header = existingRows[0];

        var existingPersonIds = existingRows
            .Select(x => x.PersonId)
            .Distinct()
            .ToHashSet();

        var desiredSet = desired.ToHashSet();

        var toRemove = existingRows
            .Where(x => !desiredSet.Contains(x.PersonId))
            .ToList();

        var toAddIds = desired
            .Where(pid => !existingPersonIds.Contains(pid))
            .ToArray();

        // 1) remove зайвих
        if (toRemove.Count > 0)
            db.CombatTaskEntries.RemoveRange(toRemove);

        // 2) add відсутніх (зі snapshot полів)
        if (toAddIds.Length > 0)
        {
            var persons = await db.PersonRead
                .AsNoTracking()
                .Where(p => toAddIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Rnokpp,
                    p.FullName,
                    p.Rank,
                    p.Position,
                    p.Weapon,
                    p.Callsign
                })
                .ToListAsync(ct);

            var found = persons.Select(x => x.Id).ToHashSet();
            var missing = toAddIds.Where(x => !found.Contains(x)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Не знайдені особи: " + string.Join(", ", missing));

            foreach (var p in persons)
            {
                db.CombatTaskEntries.Add(new CombatTaskEntry
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,

                    GroupId = groupId,
                    GroupSequence = header.GroupSequence,

                    SourceDocNo = header.SourceDocNo,
                    Action = header.Action,
                    ActionDate = header.ActionDate,

                    MissionId = header.MissionId,
                    MissionDisplaySnapshot = header.MissionDisplaySnapshot,

                    PersonId = p.Id,
                    RNOKPP = p.Rnokpp ?? string.Empty,
                    FullName = p.FullName ?? string.Empty,
                    Rank = p.Rank ?? string.Empty,
                    Position = p.Position ?? string.Empty,
                    Weapon = p.Weapon ?? string.Empty,
                    Callsign = p.Callsign ?? string.Empty
                });
            }
        }

        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }
}
