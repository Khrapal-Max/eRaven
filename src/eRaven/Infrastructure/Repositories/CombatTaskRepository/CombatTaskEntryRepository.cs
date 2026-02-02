//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Реалізація репозиторію <see cref="CombatTaskEntry"/>.
///
/// Спрощення (за бізнес-вимогою):
/// - Джерело істини для "стану особи" = останній запис на дату (ActionDate &lt;= asOfDate),
///   де "останній" визначаємо як MAX(RowNo) по PersonId.
/// - Документи зі статусом <see cref="DocumentStatus.Canceled"/> ігноруємо.
/// - Draft/Posted однаково враховуються як історія (просте правило).
///
/// Репозиторій дає мінімально необхідне для UI:
/// - CRUD по "групі" (GroupId) в межах одного документа.
/// - Списки осіб:
///   - хто зараз НА місії (для End),
///   - хто зараз ВІЛЬНИЙ (для Start).
/// </summary>
/// <remarks>Створює репозиторій.</remarks>
public sealed class CombatTaskEntryRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskEntryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Lists
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetPersonsOnMissionAsync(
        Guid missionId,
        DateOnly asOfDate,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // ---------------------------------------------------------------
        // База: історія до дати (якщо хочеш "взагалі" — просто прибери цей Where)
        // ---------------------------------------------------------------
        var baseRows = db.CombatTaskEntries
            .AsNoTracking()
            .Where(e => e.ActionDate <= asOfDate)
            .Where(e => e.Document!.Status != DocumentStatus.Canceled);

        var lastRowPerPerson = baseRows
            .GroupBy(e => e.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                RowNo = g.Max(x => x.RowNo)
            });

        var lastEntries = baseRows.Join(
            lastRowPerPerson,
            e => new { e.PersonId, e.RowNo },
            m => new { m.PersonId, m.RowNo },
            (e, _) => e
        );

        return await lastEntries
            .Where(e => e.Action == ActionKind.Start && e.MissionId == missionId)
            .OrderBy(e => e.FullName)
            .Select(e => new CombatTaskPersonLookupDto(
                PersonId: e.PersonId,
                RNOKPP: e.RNOKPP,
                FullName: e.FullName,
                Rank: e.Rank,
                Position: e.Position,
                Weapon: e.Weapon,
                Callsign: e.Callsign
            ))
            .ToListAsync(ct);
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetFreePersonsAsync(
        Guid documentId,
        DateOnly asOfDate,
        Guid? excludeGroupId = null,
        CancellationToken ct = default)
    {
        string freeMainCode = "30";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // ---------------------------------------------------------------
        // 1) База для "останнього стану" по CombatTaskEntries:
        //    - ігноруємо Canceled документи
        //    - excludeGroupId потрібен для edit: "група не впливає сама на себе"
        //    - НЕ фільтруємо по даті => "вільні взагалі" (останній запис по RowNo)
        // ---------------------------------------------------------------
        var baseRows = db.CombatTaskEntries
            .AsNoTracking()
            .Where(e => e.Document!.Status != DocumentStatus.Canceled)
            .Where(e => excludeGroupId == null || !(e.DocumentId == documentId && e.GroupId == excludeGroupId.Value));

        // ---------------------------------------------------------------
        // 2) MAX(RowNo) по PersonId
        // ---------------------------------------------------------------
        var lastRowPerPerson = baseRows
            .GroupBy(e => e.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                RowNo = g.Max(x => x.RowNo)
            });

        // ---------------------------------------------------------------
        // 3) Останній рядок по кожній особі (join по PersonId + RowNo)
        // ---------------------------------------------------------------
        var lastEntries = baseRows.Join(
            lastRowPerPerson,
            e => new { e.PersonId, e.RowNo },
            m => new { m.PersonId, m.RowNo },
            (e, _) => e
        );

        // ---------------------------------------------------------------
        // 4) Busy = останній Action == Start
        // ---------------------------------------------------------------
        var busyPersonIds = lastEntries
            .Where(e => e.Action == ActionKind.Start)
            .Select(e => e.PersonId);

        // ---------------------------------------------------------------
        // 5) Timesheet MAIN: хто НЕ доступний на дату (лікування/відпустка/ВЛК/тощо)
        //
        // Мінімальне правило для доступності:
        // - дозволяємо тільки "30" (В районі)
        // - все інше в MAIN => не доступний для планування групи
        //
        // Якщо захочеш — можна розширити дозволені коди (наприклад "0").
        // ---------------------------------------------------------------
        var notAvailableByTimesheet = db.TimesheetEntries
            .AsNoTracking()
            .Where(t => t.Lane == TimesheetLane.Main)
            .Where(t => t.From <= asOfDate)
            .Where(t => t.To == null || t.To >= asOfDate)
            .Where(t => t.Code != freeMainCode)
            .Select(t => t.PersonId);

        // ---------------------------------------------------------------
        // 6) Free = PersonRead, де НЕ busy і НЕ notAvailableByTimesheet
        // ---------------------------------------------------------------
        return await db.PersonRead
            .AsNoTracking()
            .Where(p => !busyPersonIds.Contains(p.Id))
            .Where(p => !notAvailableByTimesheet.Contains(p.Id))
            .OrderBy(p => p.FullName)
            .Select(p => new CombatTaskPersonLookupDto(
                PersonId: p.Id,
                RNOKPP: p.Rnokpp ?? string.Empty,
                FullName: p.FullName ?? string.Empty,
                Rank: p.Rank ?? string.Empty,
                Position: p.Position ?? string.Empty,
                Weapon: p.Weapon ?? string.Empty,
                Callsign: p.Callsign ?? string.Empty
            ))
            .ToListAsync(ct);
    }

    //======================================================================
    // Group CRUD
    //======================================================================

    /// <inheritdoc />
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

        // ---------------------------------------------------------------
        // Validate input minimally
        // ---------------------------------------------------------------
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(sourceDocNo)) throw new ArgumentException("SourceDocNo is required.", nameof(sourceDocNo));
        if (missionId == Guid.Empty) throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(missionDisplaySnapshot)) throw new ArgumentException("MissionDisplaySnapshot is required.", nameof(missionDisplaySnapshot));

        var distinct = personIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinct.Length == 0)
            throw new InvalidOperationException("Потрібна хоча б одна особа.");

        // ---------------------------------------------------------------
        // Load document (edit only Draft)
        // ---------------------------------------------------------------
        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        // ---------------------------------------------------------------
        // Compute latest entries for selected persons (global per person)
        // and validate action invariants (Start/End).
        // ---------------------------------------------------------------
        var baseRowsForSelected = db.CombatTaskEntries
            .AsNoTracking()
            .Where(e => distinct.Contains(e.PersonId))
            .Where(e => e.ActionDate <= actionDate)
            .Where(e => e.Document!.Status != DocumentStatus.Canceled);

        var lastRowNoForSelected = baseRowsForSelected
            .GroupBy(e => e.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                RowNo = g.Max(x => x.RowNo)
            });

        var lastForSelected = await baseRowsForSelected.Join(
                lastRowNoForSelected,
                e => new { e.PersonId, e.RowNo },
                m => new { m.PersonId, m.RowNo },
                (e, _) => new
                {
                    e.PersonId,
                    e.Action,
                    e.MissionId
                })
            .ToListAsync(ct);

        // Map to dictionary for quick checks
        var lastByPerson = lastForSelected
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.First());

        if (action == ActionKind.Start)
        {
            // Start allowed only if NOT currently on task (latest != Start)
            var busy = distinct
                .Where(pid => lastByPerson.TryGetValue(pid, out var x) && x.Action == ActionKind.Start)
                .ToArray();

            if (busy.Length > 0)
                throw new InvalidOperationException("Неможливо виконати START: частина осіб вже має активне завдання.");
        }
        else // End
        {
            // End allowed only if currently on this mission (latest == Start && MissionId == missionId)
            var invalid = distinct
                .Where(pid =>
                    !lastByPerson.TryGetValue(pid, out var x) ||
                    x.Action != ActionKind.Start ||
                    x.MissionId != missionId)
                .ToArray();

            if (invalid.Length > 0)
                throw new InvalidOperationException("Неможливо виконати END: частина осіб не знаходиться на цій місії.");
        }

        // ---------------------------------------------------------------
        // Load PersonRead snapshot (strict)
        // ---------------------------------------------------------------
        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => distinct.Contains(p.Id))
            .Select(p => new CombatTaskPersonLookupDto(
                PersonId: p.Id,
                RNOKPP: p.Rnokpp ?? string.Empty,
                FullName: p.FullName ?? string.Empty,
                Rank: p.Rank ?? string.Empty,
                Position: p.Position ?? string.Empty,
                Weapon: p.Weapon ?? string.Empty,
                Callsign: p.Callsign ?? string.Empty
            ))
            .ToListAsync(ct);

        var found = persons.Select(x => x.PersonId).ToHashSet();
        var missing = distinct.Where(x => !found.Contains(x)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("Не знайдені особи: " + string.Join(", ", missing));

        // ---------------------------------------------------------------
        // Next group sequence inside document
        // ---------------------------------------------------------------
        var nextSeq = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId)
            .Select(x => (int?)x.GroupSequence)
            .MaxAsync(ct) ?? 0;

        var groupId = Guid.NewGuid();
        var groupSeq = nextSeq + 1;

        var src = sourceDocNo.Trim();
        var display = missionDisplaySnapshot.Trim();

        // ---------------------------------------------------------------
        // Add entries: one row per person (RowNo generated by DB)
        // ---------------------------------------------------------------
        foreach (var p in persons.OrderBy(x => x.FullName))
        {
            db.CombatTaskEntries.Add(new CombatTaskEntry
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,

                GroupId = groupId,
                GroupSequence = groupSeq,

                SourceDocNo = src,
                Action = action,
                ActionDate = actionDate,

                MissionId = missionId,
                MissionDisplaySnapshot = display,

                PersonId = p.PersonId,
                RNOKPP = p.RNOKPP,
                FullName = p.FullName,
                Rank = p.Rank,
                Position = p.Position,
                Weapon = p.Weapon,
                Callsign = p.Callsign
            });
        }

        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
        return groupId;
    }

    /// <inheritdoc />
    public async Task DeleteGroupAsync(
        Guid documentId,
        Guid groupId,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("GroupId is required.", nameof(groupId));

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var rows = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return; // idempotent

        db.CombatTaskEntries.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
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

        // ---------------------------------------------------------------
        // 1) Документ (редагуємо тільки Draft)
        // ---------------------------------------------------------------
        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        // ---------------------------------------------------------------
        // 2) Рядки групи (tracked) — будемо оновлювати "шапку" однаково
        // ---------------------------------------------------------------
        var rows = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (rows.Count == 0)
            throw new InvalidOperationException("Групу не знайдено.");

        var personIds = rows
            .Select(x => x.PersonId)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        // ---------------------------------------------------------------
        // 3) Мінімальна перевірка доступності по "останньому стану" (RowNo)
        //    ВАЖЛИВО: виключаємо поточну групу, щоб не впливала сама на себе
        // ---------------------------------------------------------------
        var baseRows = db.CombatTaskEntries
            .AsNoTracking()
            .Where(e => e.Document!.Status != DocumentStatus.Canceled)
            .Where(e => personIds.Contains(e.PersonId))
            .Where(e => !(e.DocumentId == documentId && e.GroupId == groupId));

        var lastRowPerPerson = baseRows
            .GroupBy(e => e.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                RowNo = g.Max(x => x.RowNo)
            });

        var lastEntries =
            from e in baseRows
            join m in lastRowPerPerson
                on new { e.PersonId, e.RowNo }
                equals new { m.PersonId, m.RowNo }
            select e;

        if (action == ActionKind.Start)
        {
            // Start дозволений тільки якщо "останній" != Start
            var busy = await lastEntries
                .Where(e => e.Action == ActionKind.Start)
                .Select(e => e.PersonId)
                .Distinct()
                .ToListAsync(ct);

            if (busy.Count != 0)
                throw new InvalidOperationException("Неможливо виконати START: частина осіб вже має активне завдання.");
        }
        else // End
        {
            // End дозволений тільки якщо "останній" == Start і MissionId == потрібний
            var allowed = await lastEntries
                .Where(e => e.Action == ActionKind.Start && e.MissionId == missionId)
                .Select(e => e.PersonId)
                .Distinct()
                .ToListAsync(ct);

            var invalid = personIds.Except(allowed).ToArray();
            if (invalid.Length != 0)
                throw new InvalidOperationException("Неможливо виконати END: частина осіб не знаходиться на цій місії.");
        }

        // ---------------------------------------------------------------
        // 4) Оновлюємо метадані групи (всі рядки однаково)
        // ---------------------------------------------------------------
        var src = sourceDocNo.Trim();
        var display = missionDisplaySnapshot.Trim();

        foreach (var e in rows)
        {
            e.SourceDocNo = src;
            e.Action = action;
            e.ActionDate = actionDate;

            e.MissionId = missionId;
            e.MissionDisplaySnapshot = display;
        }

        // ---------------------------------------------------------------
        // 5) Audit + save
        // ---------------------------------------------------------------
        doc.UpdatedBy = author;
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task ReplaceGroupPersonsAsync(
        Guid documentId,
        Guid groupId,
        IReadOnlyCollection<Guid> personIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Завантажуємо всі рядки групи (tracked)
        var groupRows = await db.CombatTaskEntries
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .ToListAsync(ct);

        if (groupRows.Count == 0)
            throw new InvalidOperationException("Групу не знайдено.");

        // 2) Нормалізуємо список PersonIds
        var desired = personIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (desired.Length == 0)
            throw new InvalidOperationException("Потрібна хоча б одна особа.");

        // 3) Беремо “хедер” групи (метадані однакові для всіх рядків)
        var header = groupRows[0];

        // 4) Видаляємо старі рядки групи
        db.CombatTaskEntries.RemoveRange(groupRows);

        // 5) Підтягуємо PersonRead для snapshots
        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => desired.Contains(p.Id))
            .Select(p => new CombatTaskPersonLookupDto(
                PersonId: p.Id,
                RNOKPP: p.Rnokpp ?? string.Empty,
                FullName: p.FullName ?? string.Empty,
                Rank: p.Rank ?? string.Empty,
                Position: p.Position ?? string.Empty,
                Weapon: p.Weapon ?? string.Empty,
                Callsign: p.Callsign ?? string.Empty
            ))
            .ToListAsync(ct);

        var found = persons.Select(x => x.PersonId).ToHashSet();
        var missing = desired.Where(x => !found.Contains(x)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("Не знайдені особи: " + string.Join(", ", missing));

        // 6) Додаємо нові рядки групи (RowNo генерує БД)
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

                PersonId = p.PersonId,
                RNOKPP = p.RNOKPP,
                FullName = p.FullName,
                Rank = p.Rank,
                Position = p.Position,
                Weapon = p.Weapon,
                Callsign = p.Callsign
            });
        }

        // 7) Аудит документа (мінімально)
        var doc = await db.CombatTaskDocuments.FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (doc is not null)
        {
            doc.UpdatedBy = author;
            doc.UpdatedAtUtc = nowUtc;
        }

        await db.SaveChangesAsync(ct);
    }
}