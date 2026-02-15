//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// EF Core реалізація <see cref="ICombatTaskRepository"/>.
/// </summary>
public sealed class CombatTaskRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Read
    //======================================================================

    /// <inheritdoc />
    public async Task<CombatTaskEditorDto> GetDocumentEditorAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = await db.CombatTaskDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        // AsSplitQuery: уникаємо важкого JOIN-графа (особливо коли багато рядків).
        var tasks = await db.CombatTasks
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.CombatTaskDocumentId == documentId)
            .Include(x => x.Mission)
            .Include(x => x.CombatTaskDetails)
            .OrderBy(x => x.MissionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var missions = tasks
            .Select(t => new CombatTaskMissionBlockDto(
                CombatTaskId: t.Id,
                MissionId: t.MissionId,
                MissionName: t.Mission?.ToString() ?? string.Empty,
                SourceDocument: t.SourceDocument ?? string.Empty,
                CombatTaskDetails: [.. t.CombatTaskDetails
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
                        FullName: l.FullName,
                        Callsign: l.Callsign
                    ))]
            ))
            .ToList();

        return new CombatTaskEditorDto(
            DocumentId: documentId,
            DocumentName: document.OrderTitle,
            Description: document.Description,
            Status: document.Status,
            RecordedAt: document.RecordedAt,
            Missions: missions);
    }

    //======================================================================
    // Write: Create
    //======================================================================

    /// <inheritdoc />
    public async Task<Guid> CreateCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        EnsureDraft(doc);

        var exists = await db.CombatTasks
            .AsNoTracking()
            .AnyAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        if (exists)
            throw new InvalidOperationException("Блок по цій місії вже існує. Використайте UpsertCombatTaskAsync().");

        ValidateDetails(combatTaskDetails);

        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = (sourceDocument ?? string.Empty).Trim()
        };

        db.CombatTasks.Add(task);

        PrepareLinesForInsert(task.Id, combatTaskDetails);
        db.CombatTaskDetails.AddRange(combatTaskDetails);

        await db.SaveChangesAsync(ct);
        return task.Id;
    }

    //======================================================================
    // Write: Upsert (replace details)
    //======================================================================

    /// <inheritdoc />
    public async Task<Guid> UpsertCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        EnsureDraft(doc);

        ValidateDetails(combatTaskDetails);

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .FirstOrDefaultAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        if (task is null)
        {
            task = new CombatTask
            {
                Id = Guid.NewGuid(),
                CombatTaskDocumentId = documentId,
                MissionId = missionId,
                SourceDocument = (sourceDocument ?? string.Empty).Trim()
            };

            db.CombatTasks.Add(task);

            PrepareLinesForInsert(task.Id, combatTaskDetails);
            db.CombatTaskDetails.AddRange(combatTaskDetails);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return task.Id;
        }

        // Update header
        task.SourceDocument = (sourceDocument ?? string.Empty).Trim();

        // Replace details (atomic)
        if (task.CombatTaskDetails.Count > 0)
            db.CombatTaskDetails.RemoveRange(task.CombatTaskDetails);

        PrepareLinesForInsert(task.Id, combatTaskDetails);
        db.CombatTaskDetails.AddRange(combatTaskDetails);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return task.Id;
    }

    //======================================================================
    // Write: Delete
    //======================================================================

    /// <inheritdoc />
    public async Task DeleteCombatTaskAsync(Guid documentId, Guid combatTaskId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (combatTaskId == Guid.Empty)
            throw new ArgumentException("CombatTaskId is required.", nameof(combatTaskId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId, ct);

        if (doc is null)
            return;

        EnsureDraft(doc);

        var task = await db.CombatTasks
            .FirstOrDefaultAsync(x => x.Id == combatTaskId && x.CombatTaskDocumentId == documentId, ct);

        if (task is null)
            return;

        db.CombatTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Забороняє зміну контенту, якщо документ не у Draft.
    /// Це захист цілісності аудиту.
    /// </summary>
    private static void EnsureDraft(CombatTaskDocument doc)
    {
        if (doc.Status == DocumentStatus.Posted)
            throw new InvalidOperationException("Документ проведений і не може редагуватися.");
        if (doc.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може редагуватися.");
    }

    /// <summary>
    /// Приводить lines до інваріанту перед insert:
    /// <list type="bullet">
    /// <item><description>гарантує <c>Id</c>;</description></item>
    /// <item><description>проставляє FK <c>CombatTaskId</c>;</description></item>
    /// <item><description>скидає navigation, щоб EF не тягнув зайвий граф.</description></item>
    /// </list>
    /// </summary>
    private static void PrepareLinesForInsert(Guid combatTaskId, IEnumerable<CombatTaskDetails> lines)
    {
        foreach (var line in lines)
        {
            if (line.Id == Guid.Empty)
                line.Id = Guid.NewGuid();

            line.CombatTaskId = combatTaskId;
            line.CombatTask = null;
        }
    }

    /// <summary>
    /// Мінімальна перевірка цілісності рядків:
    /// <list type="bullet">
    /// <item><description><c>PersonId</c> / <c>EffectiveAt</c> заповнені;</description></item>
    /// <item><description>snapshot поля <c>Rnokpp/FullName</c> заповнені;</description></item>
    /// <item><description>в межах одного CombatTask немає дублікатів (PersonId, Kind, EffectiveAt).</description></item>
    /// </list>
    /// </summary>
    private static void ValidateDetails(IReadOnlyCollection<CombatTaskDetails> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
            return;

        var dup = new HashSet<(Guid PersonId, CombatTaskDetailsKind Kind, DateOnly EffectiveAt)>();

        foreach (var x in lines)
        {
            if (x.PersonId == Guid.Empty)
                throw new InvalidOperationException("Рядок містить порожній PersonId.");

            if (x.EffectiveAt == default)
                throw new InvalidOperationException("Рядок містить некоректну EffectiveAt дату.");

            if (string.IsNullOrWhiteSpace(x.Rnokpp))
                throw new InvalidOperationException("Rnokpp обов'язковий для рядка (light snapshot).");

            if (string.IsNullOrWhiteSpace(x.FullName))
                throw new InvalidOperationException("FullName обов'язковий для рядка (light snapshot).");

            var key = (x.PersonId, x.Kind, x.EffectiveAt);
            if (!dup.Add(key))
            {
                throw new InvalidOperationException(
                    $"Дублікат рядка: PersonId={x.PersonId}, Kind={x.Kind}, EffectiveAt={x.EffectiveAt:yyyy-MM-dd}.");
            }
        }
    }
}
