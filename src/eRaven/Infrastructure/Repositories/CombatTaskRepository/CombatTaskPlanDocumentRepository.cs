//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public sealed class CombatTaskPlanDocumentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ICombatTaskPlanDocumentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<Guid> CreateDraftAsync(
        DateOnly recordedAt,
        DateOnly planningDate,
        string planningDocTitle,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(planningDocTitle))
            throw new ArgumentException("PlanningDocTitle is required.", nameof(planningDocTitle));

        if (string.IsNullOrWhiteSpace(author))
            author = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            RecordedAt = recordedAt,
            PlanningDate = planningDate,
            PlanningDocTitle = planningDocTitle.Trim(),
            Status = CombatTaskPlanDocumentStatus.Draft,
            CreatedBy = author.Trim(),
            CreatedAtUtc = nowUtc
        };

        db.CombatTaskPlanDocuments.Add(doc);

        await db.SaveChangesAsync(ct);

        return doc.Id;
    }

    public async Task PostAsync(Guid documentId, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(author)) author = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var doc = await db.Set<CombatTaskPlanDocument>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status == CombatTaskPlanDocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може бути проведений.");

        if (doc.Status == CombatTaskPlanDocumentStatus.Posted)
            return; // ідемпотентність (щоб не дублювати)

        // важливо: проводимо саме Lines документа
        foreach (var line in doc.Lines
                                .OrderBy(x => x.ActionDate)
                                .ThenByDescending(x => x.Kind)) // End(1) before Start(0)
        {
            if (line.Kind == CombatTaskPlanLineKind.Start)
            {
                // Правило: не можна стартувати, якщо є відкрите завдання
                var open = await db.CombatTaskAssignments
                    .FirstOrDefaultAsync(x => x.PersonId == line.PersonId && x.EndedAt == null, ct);

                if (open is not null)
                    throw new InvalidOperationException($"Особа вже має активне завдання з {open.StartedAt:dd.MM.yyyy}. Спочатку закрийте його.");

                // Створюємо assignment з деталей лінії + документа
                var a = new CombatTaskAssignment
                {
                    Id = line.AssignmentId == Guid.Empty ? Guid.NewGuid() : line.AssignmentId,
                    PersonId = line.PersonId,

                    StartedAt = line.ActionDate,
                    EndedAt = null,

                    StartDocumentId = doc.Id,
                    EndDocumentId = null,

                    PlanningDate = doc.PlanningDate,
                    PlanningDocTitle = doc.PlanningDocTitle,

                    PositionalArea = line.PositionalArea,
                    GroupName = line.GroupName,
                    AssetType = line.AssetType,
                    Mode = line.Mode,
                    Goal = line.Goal,
                    IsActual = true,

                    RNOKPP = line.RNOKPP,
                    FullName = line.FullName,
                    Rank = line.Rank,
                    Position = line.Position,
                    Weapon = line.Weapon,
                    Callsign = line.Callsign,

                    CreatedBy = author.Trim(),
                    CreatedAtUtc = nowUtc
                };

                // якщо у draft генерували AssignmentId — він має співпасти
                line.AssignmentId = a.Id;

                db.CombatTaskAssignments.Add(a);
            }
            else // End
            {
                // Закриваємо тільки відкрите завдання
                var open = await db.CombatTaskAssignments
                    .FirstOrDefaultAsync(x => x.PersonId == line.PersonId && x.EndedAt == null, ct) ?? throw new InvalidOperationException($"Немає активного завдання для закриття (особа: {line.FullName}).");

                // Якщо в лінії вказали AssignmentId — перевіряємо, що закриваємо саме його
                if (line.AssignmentId != Guid.Empty && line.AssignmentId != open.Id)
                    throw new InvalidOperationException("Вказаний AssignmentId не відповідає активному завданню особи.");

                // Дозволяємо “закрити в той же день”, коли почалось (внутрішньоденний перехід)
                if (line.ActionDate < open.StartedAt)
                    throw new InvalidOperationException("Дата закриття не може бути раніше дати початку.");

                open.EndedAt = line.ActionDate;       // семантика: “дата закриття/переходу”
                open.EndDocumentId = doc.Id;
                open.IsActual = false;
                open.UpdatedBy = author.Trim();
                open.UpdatedAtUtc = nowUtc;

                line.AssignmentId = open.Id;          // фіксуємо, який саме ланцюжок закрили
            }
        }

        doc.Status = CombatTaskPlanDocumentStatus.Posted;
        doc.UpdatedBy = author.Trim();
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task CancelAsync(Guid documentId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(author)) author = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskPlanDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status == CombatTaskPlanDocumentStatus.Posted)
            throw new InvalidOperationException("Проведений документ поки не відміняємо (потрібен механізм компенсації).");

        doc.Status = CombatTaskPlanDocumentStatus.Canceled;
        doc.CanceledReason = reason.Trim();
        doc.CanceledBy = author.Trim();
        doc.CanceledAtUtc = nowUtc;
        doc.UpdatedBy = author.Trim();
        doc.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateLine(CombatTaskPlanLineInputDto l)
    {
        if (l.PersonId == Guid.Empty) throw new ArgumentException("Line.PersonId is required.");
        if (string.IsNullOrWhiteSpace(l.RNOKPP)) throw new ArgumentException("Line.RNOKPP is required.");
        if (string.IsNullOrWhiteSpace(l.FullName)) throw new ArgumentException("Line.FullName is required.");
        if (string.IsNullOrWhiteSpace(l.PositionalArea)) throw new ArgumentException("Line.PositionalArea is required.");
        if (string.IsNullOrWhiteSpace(l.GroupName)) throw new ArgumentException("Line.GroupName is required.");
        if (string.IsNullOrWhiteSpace(l.Goal)) throw new ArgumentException("Line.Goal is required.");
    }
}
