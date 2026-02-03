//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionParticipationRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public sealed class MissionParticipationRepository(IDbContextFactory<AppDbContext> dbFactory)
    : IMissionParticipationRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Queries
    //======================================================================

    //======================================================================
    // Day report
    //======================================================================

    public async Task<IReadOnlyList<MissionParticipationRowDto>> GetOnDateAsync(
        DateOnly date,
        Guid? missionId,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.MissionParticipations
            .AsNoTracking()
            .Where(x => x.From <= date && (x.To == null || x.To >= date));

        q = ApplyFilters(q, missionId, search);

        return await q.Select(x => new MissionParticipationRowDto(
            DocumentId: x.DocumentId,
            GroupId: x.GroupId,
            GroupSequence: x.GroupSequence,

            MissionId: x.MissionId,
            MissionDisplaySnapshot: x.MissionDisplaySnapshot,

            PersonId: x.PersonId,
            RNOKPP: x.RNOKPP,
            FullName: x.FullName,
            Rank: x.Rank,
            Position: x.Position,
            Weapon: x.Weapon,
            Callsign: x.Callsign,

            From: x.From,
            To: x.To,
            SourceDocNo: x.SourceDocNo,
            EndSourceDocNo: x.EndSourceDocNo
        ))
        .OrderBy(x => x.MissionDisplaySnapshot)
        .ThenBy(x => x.FullName)
        .ToListAsync(ct);
    }

    //======================================================================
    // Month report
    //======================================================================

    public async Task<IReadOnlyList<MissionParticipationRowDto>> GetOverlappingMonthAsync(
        int year,
        int month,
        Guid? missionId,
        string? search,
        CancellationToken ct = default)
    {
        var y = Math.Clamp(year, 2000, 2100);
        var m = Math.Clamp(month, 1, 12);

        var from = new DateOnly(y, m, 1);
        var to = from.AddMonths(1).AddDays(-1); // inclusive

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.MissionParticipations
            .AsNoTracking()
            .Where(x => x.From <= to && (x.To == null || x.To >= from));

        q = ApplyFilters(q, missionId, search);

        return await q.Select(x => new MissionParticipationRowDto(
            DocumentId: x.DocumentId,
            GroupId: x.GroupId,
            GroupSequence: x.GroupSequence,

            MissionId: x.MissionId,
            MissionDisplaySnapshot: x.MissionDisplaySnapshot,

            PersonId: x.PersonId,
            RNOKPP: x.RNOKPP,
            FullName: x.FullName,
            Rank: x.Rank,
            Position: x.Position,
            Weapon: x.Weapon,
            Callsign: x.Callsign,

            From: x.From,
            To: x.To,
            SourceDocNo: x.SourceDocNo,
            EndSourceDocNo: x.EndSourceDocNo
        ))
        .OrderBy(x => x.MissionDisplaySnapshot)
        .ThenBy(x => x.FullName)
        .ToListAsync(ct);
    }

    //======================================================================
    // Busy persons (for picker)
    //======================================================================

    public async Task<IReadOnlyList<Guid>> GetBusyPersonIdsOnDateAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.MissionParticipations
            .AsNoTracking()
            .Where(x => x.From <= date && (x.To == null || x.To >= date))
            .Select(x => x.PersonId)
            .Distinct()
            .ToListAsync(ct);
    }

    //======================================================================
    // Internal helpers
    //======================================================================

    /// <summary>
    /// Загальні фільтри по місії та по search (snapshot).
    /// </summary>
    private static IQueryable<MissionParticipation> ApplyFilters(
        IQueryable<MissionParticipation> q,
        Guid? missionId,
        string? search)
    {
        if (missionId is Guid mid && mid != Guid.Empty)
            q = q.Where(x => x.MissionId == mid);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                x.FullName.Contains(s) ||
                x.RNOKPP.Contains(s) ||
                x.Callsign.Contains(s));
        }

        return q;
    }

    //======================================================================
    // Commands: Start (Create group)
    //======================================================================

    public async Task<Guid> StartGroupAsync(
        Guid documentId,
        string sourceDocNo,
        Guid missionId,
        string missionDisplaySnapshot,
        DateOnly from,
        IReadOnlyCollection<CombatTaskPersonLookupDto> persons,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId is required.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId is required.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(sourceDocNo)) throw new ArgumentException("sourceDocNo is required.", nameof(sourceDocNo));

        author = string.IsNullOrWhiteSpace(author) ? "system" : author.Trim();

        var list = (persons ?? [])
            .Where(p => p.PersonId != Guid.Empty)
            .GroupBy(p => p.PersonId)
            .Select(g => g.First())
            .ToList();

        if (list.Count == 0)
            throw new InvalidOperationException("Не вибрано осіб.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var personIds = list.Select(x => x.PersonId).ToArray();

        // Інваріант: 1 активна участь на особу (To IS NULL)
        var hasActive = await db.MissionParticipations
            .AnyAsync(x => personIds.Contains(x.PersonId) && x.To == null, ct);

        if (hasActive)
            throw new InvalidOperationException("Деякі особи вже мають активне завдання (відкритий інтервал). Спочатку закрийте участь.");

        var groupId = Guid.NewGuid();

        // GroupSequence = max+1 в межах документа (достатньо для друку/UX)
        var nextSeq = await db.MissionParticipations
            .Where(x => x.DocumentId == documentId)
            .Select(x => (int?)x.GroupSequence)
            .MaxAsync(ct) ?? 0;

        var seq = nextSeq + 1;

        var srcNo = sourceDocNo.Trim();
        var missionDisplay = (missionDisplaySnapshot ?? string.Empty).Trim();

        foreach (var p in list)
        {
            db.MissionParticipations.Add(new MissionParticipation
            {
                Id = Guid.NewGuid(),

                DocumentId = documentId,
                GroupId = groupId,
                GroupSequence = seq,

                SourceDocNo = srcNo,

                MissionId = missionId,
                MissionDisplaySnapshot = missionDisplay,

                PersonId = p.PersonId,
                RNOKPP = p.RNOKPP,
                FullName = p.FullName,
                Rank = p.Rank,
                Position = p.Position,
                Weapon = p.Weapon,
                Callsign = p.Callsign,

                From = from,
                To = null,

                CreatedBy = author,
                CreatedAtUtc = nowUtc
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return groupId;
    }

    //======================================================================
    // Commands: End (Close group)
    //======================================================================

    public async Task EndGroupAsync(
        Guid documentId,
        Guid groupId,
        DateOnly to,
        string endSourceDocNo,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("groupId is required.", nameof(groupId));
        if (string.IsNullOrWhiteSpace(endSourceDocNo)) throw new ArgumentException("endSourceDocNo is required.", nameof(endSourceDocNo));

        author = string.IsNullOrWhiteSpace(author) ? "system" : author.Trim();
        var endNo = endSourceDocNo.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Закриваємо тільки відкриті рядки групи
        var openRows = await db.MissionParticipations
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId && x.To == null)
            .ToListAsync(ct);

        if (openRows.Count == 0)
            throw new InvalidOperationException("Немає відкритих участей у цій групі для закриття.");

        foreach (var r in openRows)
        {
            if (to < r.From)
                throw new InvalidOperationException("Дата завершення не може бути раніше дати початку.");

            r.To = to;
            r.EndSourceDocNo = endNo;
            r.UpdatedBy = author;
            r.UpdatedAtUtc = nowUtc;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Commands: Delete (Draft cleanup)
    //======================================================================

    public async Task DeleteGroupAsync(Guid documentId, Guid groupId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("documentId is required.", nameof(documentId));
        if (groupId == Guid.Empty) throw new ArgumentException("groupId is required.", nameof(groupId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Вибираємо лише PK, щоб не тягнути весь snapshot
        var ids = await db.MissionParticipations
            .Where(x => x.DocumentId == documentId && x.GroupId == groupId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        foreach (var id in ids)
            db.MissionParticipations.Remove(new MissionParticipation { Id = id });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
