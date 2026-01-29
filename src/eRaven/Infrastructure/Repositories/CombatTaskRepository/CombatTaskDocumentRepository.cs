//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

// TODO need tests
public class CombatTaskDocumentRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskDocumentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<CombatTaskDocumentDto>> GetDocumentsAsync(
        int year,
        int month,
        DocumentStatus? status,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        var query = db.CombatTaskDocuments
            .AsNoTracking()
            .Where(d => d.RecordedAt >= from && d.RecordedAt < to);

        if (status is not null)
            query = query.Where(d => d.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.OrderTitle.Contains(s));
        }

        return await query
            .Select(d => new CombatTaskDocumentDto(
                DocumentId: d.Id,
                OrderTitle: d.OrderTitle,
                Status: d.Status,
                RecordedAt: d.RecordedAt,
                CanceledReason: d.CanceledReason ?? string.Empty))
            .ToListAsync(ct);
    }

    public async Task<CombatTaskDocumentDetailsDto?> GetByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var header = await db.CombatTaskDocuments
            .AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => new
            {
                x.Id,
                x.OrderTitle,
                x.Status,
                x.RecordedAt,
                x.CanceledReason
            })
            .FirstOrDefaultAsync(ct);

        if (header is null)
            return null;

        // 1) Дії + місія
        var actions = await db.MissionActions
            .AsNoTracking()
            .Where(a => a.DocumentId == documentId)
            .Join(db.Missions.AsNoTracking(),
                a => a.MissionId,
                m => m.Id,
                (a, m) => new
                {
                    a.Id,
                    a.Sequence,
                    a.SourceDocNo,
                    a.Action,
                    a.MissionId,
                    a.ActionDate,
                    MissionDisplay = m.PositionArea
                        + (m.NamePoint != null ? $" / {m.NamePoint}" : "")
                        + $" / {m.Target} / {m.MissionMode}"
                        + (m.TypeDrone != null ? $" / {m.TypeDrone}" : "")
                })
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);

        if (actions.Count == 0)
        {
            return new CombatTaskDocumentDetailsDto(
                DocumentId: header.Id,
                OrderTitle: header.OrderTitle,
                Status: header.Status,
                RecordedAt: header.RecordedAt,
                CanceledReason: header.CanceledReason ?? string.Empty,
                Actions: []);
        }

        var actionIds = actions.Select(x => x.Id).ToArray();

        // 2) action -> persons + SNAPSHOT FIELDS ARE HERE NOW
        var links = await db.MissionActionPersons
            .AsNoTracking()
            .Where(x => actionIds.Contains(x.ActionId))
            .ToListAsync(ct);

        // 3) Групування: одна дія -> список людей (беремо прямо зі снапшота в join-table)
        var details = actions
            .Select(a =>
            {
                var persons = links
                    .Where(l => l.ActionId == a.Id)
                    .Select(l => new MissionActionPersonDto(
                        PersonId: l.PersonId,
                        RNOKPP: l.RNOKPP,
                        FullName: l.FullName,
                        Rank: l.Rank,
                        Position: l.Position,
                        Weapon: l.Weapon,
                        Callsign: l.Callsign))
                    .OrderBy(p => p.FullName)
                    .ToList();

                return new MissionActionDetailsDto(
                    ActionId: a.Id,
                    Sequence: a.Sequence,
                    SourceDocNo: a.SourceDocNo,
                    Action: a.Action,
                    MissionId: a.MissionId,
                    MissionDisplay: a.MissionDisplay,
                    ActionDate: a.ActionDate,
                    Persons: persons);
            })
            .ToList();

        return new CombatTaskDocumentDetailsDto(
            DocumentId: header.Id,
            OrderTitle: header.OrderTitle,
            Status: header.Status,
            RecordedAt: header.RecordedAt,
            CanceledReason: header.CanceledReason ?? string.Empty,
            Actions: details);
    }

    public async Task<Guid> CreateDraftAsync(
        string orderTitle,
        DateOnly recordedAt,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = new CombatTaskDocument
        {
            Id = Guid.NewGuid(),
            OrderTitle = orderTitle,
            RecordedAt = recordedAt,
            Status = DocumentStatus.Draft,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        };

        db.CombatTaskDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return document.Id;
    }

    public async Task PostAsync(Guid documentId, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (document.Status == DocumentStatus.Posted)
            return;

        if (document.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може бути проведений.");

        document.Status = DocumentStatus.Posted;
        document.UpdatedBy = author;
        document.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid documentId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (document.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Документ вже проведений.");

        if (document.Status == DocumentStatus.Canceled)
            return;

        document.Status = DocumentStatus.Canceled;
        document.CanceledReason = reason;
        document.CreatedBy = author;
        document.CanceledAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddActionAsync(
        Guid documentId,
        string sourceDocNo,
        ActionKind action,
        Guid missionId,
        DateOnly actionDate,
        IReadOnlyCollection<Guid> personIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (missionId == Guid.Empty) throw new ArgumentException("MissionId is required.", nameof(missionId));
        if (string.IsNullOrWhiteSpace(sourceDocNo)) throw new ArgumentException("SourceDocNo is required.", nameof(sourceDocNo));
        if (personIds is null || personIds.Count == 0) throw new ArgumentException("At least one person is required.", nameof(personIds));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var distinctPersonIds = personIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctPersonIds.Length == 0)
            throw new ArgumentException("At least one valid person is required.", nameof(personIds));

        // 1) Наступний sequence
        var nextSeq = await db.MissionActions
            .Where(x => x.DocumentId == documentId)
            .Select(x => (int?)x.Sequence)
            .MaxAsync(ct) ?? 0;

        // 2) Створюємо action
        var act = new MissionAction
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = nextSeq + 1,
            SourceDocNo = sourceDocNo.Trim(),
            Action = action,
            MissionId = missionId,
            ActionDate = actionDate
        };

        db.MissionActions.Add(act);

        // 3) SNAPSHOT на Action×Person: беремо поточні дані особи і пишемо у join-table
        // !!! заміни PersonReadModel на свій реальний read-model (DbSet)
        var persons = await db.PersonRead
            .AsNoTracking()
            .Where(p => distinctPersonIds.Contains(p.Id))
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
        var notFound = distinctPersonIds.Where(x => !found.Contains(x)).ToArray();
        if (notFound.Length > 0)
            throw new InvalidOperationException("Не знайдені особи: " + string.Join(", ", notFound));

        foreach (var p in persons)
        {
            db.MissionActionPersons.Add(new MissionActionPerson
            {
                ActionId = act.Id,
                PersonId = p.Id,

                // snapshot fields
                RNOKPP = p.Rnokpp ?? string.Empty,
                FullName = p.FullName ?? string.Empty,
                Rank = p.Rank ?? string.Empty,
                Position = p.Position ?? string.Empty,
                Weapon = p.Weapon ?? string.Empty,
                Callsign = p.Callsign ?? string.Empty
            });
        }

        // 4) Audit
        doc.UpdatedBy = author.Trim();
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
        return act.Id;
    }

    public async Task DeleteActionAsync(Guid documentId, Guid actionId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (actionId == Guid.Empty) throw new ArgumentException("ActionId is required.", nameof(actionId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status != DocumentStatus.Draft)
            throw new InvalidOperationException("Редагувати можна лише чернетку.");

        var act = await db.MissionActions
            .FirstOrDefaultAsync(x => x.Id == actionId && x.DocumentId == documentId, ct)
            ?? throw new InvalidOperationException("Дію не знайдено.");

        // remove join rows first (навіть якщо є cascade — так прозоріше)
        var links = await db.MissionActionPersons
            .Where(x => x.ActionId == act.Id)
            .ToListAsync(ct);

        db.MissionActionPersons.RemoveRange(links);
        db.MissionActions.Remove(act);

        await db.SaveChangesAsync(ct);
    }
}