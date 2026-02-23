//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій контенту документа бойових завдань:
/// <see cref="CombatTask"/> та <see cref="CombatTaskDetails"/>.
///
/// <para>
/// Спрощена модель: документ одразу чинний (<see cref="DocumentStatus.Active"/>) і формує факт у табелі;
/// чернеток/Posted немає; факт не видаляємо — тільки компенсація через Cancel.
/// </para>
///
/// <para>
/// <b>Примітка:</b> репозиторій зберігає "контент документа" як immutable-snapshot.
/// Тому оновлення робимо у стилі replace-all для details.
/// </para>
/// </summary>
public sealed class CombatTaskRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskRepository
{
    private const int SourceDocumentMaxLength = 128;

    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Read: Editor
    //======================================================================

    /// <inheritdoc />
    public async Task<CombatTaskDocument> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.CombatTaskDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.CombatTasks)
            .ThenInclude(x => x.CombatTaskDetails)
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetDocumentMissionIdsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.CombatTasks
            .AsNoTracking()
            .Where(x => x.CombatTaskDocumentId == documentId)
            .Select(x => x.MissionId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
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

        var source = EnsureSourceDocument(sourceDocument);
        EnsureDetailsNotEmpty(combatTaskDetails);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var status = await GetDocumentStatusAsync(db, documentId, ct);

        if (status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ скасовано. Редагування заборонено.");

        var exists = await db.CombatTasks
            .AsNoTracking()
            .AnyAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        if (exists)
            throw new InvalidOperationException("Завдання для цієї місії вже існує у документі.");

        var taskId = Guid.NewGuid();

        var task = new CombatTask
        {
            Id = taskId,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = source,
            CombatTaskDetails = []
        };

        foreach (var row in combatTaskDetails)
            task.CombatTaskDetails.Add(CloneDetail(row, taskId));

        db.CombatTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return taskId;
    }

    //======================================================================
    // Write: Upsert
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

        var source = EnsureSourceDocument(sourceDocument);
        EnsureDetailsNotEmpty(combatTaskDetails);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var status = await GetDocumentStatusAsync(db, documentId, ct);

        if (status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ скасовано. Редагування заборонено.");

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .SingleOrDefaultAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        if (task is null)
        {
            task = new CombatTask
            {
                Id = Guid.NewGuid(),
                CombatTaskDocumentId = documentId,
                MissionId = missionId,
                SourceDocument = source,
                CombatTaskDetails = []
            };

            db.CombatTasks.Add(task);
        }
        else
        {
            task.SourceDocument = source;

            // Replace-all: remove old snapshot rows.
            db.CombatTaskDetails.RemoveRange(task.CombatTaskDetails);
            task.CombatTaskDetails.Clear();
        }

        foreach (var row in combatTaskDetails)
            task.CombatTaskDetails.Add(CloneDetail(row, task.Id));

        await db.SaveChangesAsync(ct);
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

        var status = await GetDocumentStatusAsync(db, documentId, ct);

        if (status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ скасовано. Редагування заборонено.");

        var task = await db.CombatTasks
            .FirstOrDefaultAsync(x => x.Id == combatTaskId && x.CombatTaskDocumentId == documentId, ct);

        if (task is null)
            return;

        db.CombatTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    //======================================================================
    // Internals
    //======================================================================

    /// <summary>
    /// Повертає статус документа або кидає помилку, якщо документ не знайдено.
    /// </summary>
    private static async Task<DocumentStatus> GetDocumentStatusAsync(AppDbContext db, Guid documentId, CancellationToken ct)
    {
        var status = await db.CombatTaskDocuments
            .AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => (DocumentStatus?)x.Status)
            .SingleOrDefaultAsync(ct);

        return status ?? throw new InvalidOperationException("Документ не знайдено.");
    }

    /// <summary>
    /// Нормалізує <paramref name="sourceDocument"/>: trim + валідація.
    /// </summary>
    private static string EnsureSourceDocument(string sourceDocument)
    {
        if (string.IsNullOrWhiteSpace(sourceDocument))
            throw new ArgumentException("SourceDocument is required.", nameof(sourceDocument));

        var value = sourceDocument.Trim();

        if (value.Length > SourceDocumentMaxLength)
            throw new InvalidOperationException($"SourceDocument must be <= {SourceDocumentMaxLength} chars.");

        return value;
    }

    /// <summary>
    /// Гарантує, що details не null та не порожні.
    /// </summary>
    private static void EnsureDetailsNotEmpty(IReadOnlyCollection<CombatTaskDetails> combatTaskDetails)
    {
        ArgumentNullException.ThrowIfNull(combatTaskDetails);

        if (combatTaskDetails.Count == 0)
            throw new InvalidOperationException("combatTaskDetails cannot be empty.");
    }

    /// <summary>
    /// Створює новий snapshot-рядок для поточного CombatTask, не мутуючи вхідний об'єкт.
    /// </summary>
    private static CombatTaskDetails CloneDetail(CombatTaskDetails row, Guid combatTaskId)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new CombatTaskDetails
        {
            Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id,
            CombatTaskId = combatTaskId,
            Kind = row.Kind,
            EffectiveAt = row.EffectiveAt,
            PersonId = row.PersonId == Guid.Empty ? Guid.NewGuid() : row.PersonId,
            Rnokpp = row.Rnokpp,
            FullName = row.FullName,
            Rank = row.Rank,
            Position = row.Position,
            Weapon = row.Weapon,
            Callsign = row.Callsign
        };
    }
}
