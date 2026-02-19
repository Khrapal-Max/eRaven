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
/// Спрощена модель: документ одразу чинний (Active) і формує факт у табелі;
/// чернеток/Posted немає; факт не видаляємо — тільки компенсація через Cancel.
/// </para>
/// </summary>
public sealed class CombatTaskRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskRepository
{
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

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може редагуватися.");

        var exists = await db.CombatTasks
            .AsNoTracking()
            .AnyAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        if (exists)
            throw new InvalidOperationException("Завдання для цієї місії вже існує у документі.");

        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = sourceDocument.Trim(),
            CombatTaskDetails = [.. combatTaskDetails]
        };

        db.CombatTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task.Id;
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
        if (string.IsNullOrWhiteSpace(sourceDocument))
            throw new ArgumentException("SourceDocument is required.", nameof(sourceDocument));
        ArgumentNullException.ThrowIfNull(combatTaskDetails);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може редагуватися.");

        var task = await db.CombatTasks
            .Include(x => x.CombatTaskDetails)
            .FirstOrDefaultAsync(x => x.CombatTaskDocumentId == documentId && x.MissionId == missionId, ct);

        // Матеріалізуємо та нормалізуємо рядки (FK + валідація ключа)
        static List<CombatTaskDetails> NormalizeDetails(Guid combatTaskId, IEnumerable<CombatTaskDetails> rows)
        {
            var list = rows.ToList();

            foreach (var d in list)
            {
                if (d.Id == Guid.Empty)
                    throw new InvalidOperationException("CombatTaskDetails.Id must be set (non-empty).");

                d.CombatTaskId = combatTaskId;
                d.CombatTask = null; // уникнути перенесення навігації з інших контекстів
            }

            return list;
        }

        if (task is null)
        {
            task = new CombatTask
            {
                Id = Guid.NewGuid(),
                CombatTaskDocumentId = documentId,
                MissionId = missionId,
                SourceDocument = sourceDocument.Trim(),
                CombatTaskDetails = []
            };

            var newDetails = NormalizeDetails(task.Id, combatTaskDetails);

            // Add(task) протягне граф як Added, але ми все одно явно додаємо залежні,
            // щоб не залежати від евристик EF щодо ключів.
            db.CombatTasks.Add(task);
            db.CombatTaskDetails.AddRange(newDetails);
            task.CombatTaskDetails = newDetails;
        }
        else
        {
            task.SourceDocument = sourceDocument.Trim();

            // replace-all: DELETE старих + INSERT нових
            db.CombatTaskDetails.RemoveRange(task.CombatTaskDetails);

            var newDetails = NormalizeDetails(task.Id, combatTaskDetails);

            // ВАЖЛИВО: для Guid-ключів EF може помилково трактувати рядки як "існуючі"
            // і спробувати UPDATE замість INSERT => concurrency exception.
            db.CombatTaskDetails.AddRange(newDetails);
            task.CombatTaskDetails = newDetails;
        }

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

        var doc = await db.CombatTaskDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId, ct)
            ?? throw new InvalidOperationException("Документ не знайдено.");

        if (doc.Status == DocumentStatus.Canceled)
            throw new InvalidOperationException("Документ відмінений і не може редагуватися.");

        var task = await db.CombatTasks
            .FirstOrDefaultAsync(x => x.Id == combatTaskId && x.CombatTaskDocumentId == documentId, ct);

        if (task is null)
            return;

        db.CombatTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }
}
