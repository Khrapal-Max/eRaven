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
/// <b>Примітка:</b> у системі <see cref="CombatTaskDetails.Id"/> є стабільним ідентифікатором факту (використовується як source-id в інтервалах).
/// Тому при оновленні ми синхронізуємо snapshot-рядки по Id (upsert/delete), не змінюючи Id.
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
        EnsureDetailsAreStable(combatTaskDetails);

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
        EnsureDetailsAreStable(combatTaskDetails);

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
        }

        // Upsert snapshot rows by stable Id to avoid EF tracking conflicts.
        var incomingById = combatTaskDetails.ToDictionary(x => x.Id);
        var existingById = task.CombatTaskDetails.ToDictionary(x => x.Id);

        // Update existing + add new
        foreach (var row in combatTaskDetails)
        {
            if (!existingById.TryGetValue(row.Id, out var existing))
            {
                task.CombatTaskDetails.Add(CloneDetail(row, task.Id));
                continue;
            }

            // Update tracked entity (Id is stable)
            existing.Kind = row.Kind;
            existing.EffectiveAt = row.EffectiveAt;
            existing.PersonId = row.PersonId;

            existing.Rnokpp = row.Rnokpp;
            existing.FullName = row.FullName;
            existing.Rank = row.Rank;
            existing.Position = row.Position;
            existing.Weapon = row.Weapon;
            existing.Callsign = row.Callsign;
        }

        // Remove missing
        var toRemove = task.CombatTaskDetails.Where(x => !incomingById.ContainsKey(x.Id)).ToList();
        if (toRemove.Count > 0)
        {
            db.CombatTaskDetails.RemoveRange(toRemove);
            foreach (var r in toRemove)
                task.CombatTaskDetails.Remove(r);
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
    /// Гарантує стабільні ідентифікатори snapshot-рядків.
    ///
    /// <para>
    /// У системі <see cref="CombatTaskDetails.Id"/> є ключем ідемпотентності (використовується як <c>Source*DetailsId</c> у <c>MissionAssignment</c>).
    /// Тому <b>Guid.Empty недопустимий</b>, а Id повинні бути унікальні в межах запиту.
    /// </para>
    /// </summary>
    private static void EnsureDetailsAreStable(IReadOnlyCollection<CombatTaskDetails> combatTaskDetails)
    {
        var ids = new HashSet<Guid>();

        foreach (var row in combatTaskDetails)
        {
            ArgumentNullException.ThrowIfNull(row);

            if (row.Id == Guid.Empty)
                throw new InvalidOperationException("CombatTaskDetails.Id must be set (stable Id is required).");

            if (!ids.Add(row.Id))
                throw new InvalidOperationException("CombatTaskDetails.Id must be unique within the request.");

            if (row.PersonId == Guid.Empty)
                throw new InvalidOperationException("CombatTaskDetails.PersonId must be set.");

            if (row.EffectiveAt == default)
                throw new InvalidOperationException("CombatTaskDetails.EffectiveAt must be set.");
        }
    }

    /// <summary>
    /// Створює новий snapshot-рядок для поточного CombatTask, не мутуючи вхідний об'єкт.
    /// </summary>
    private static CombatTaskDetails CloneDetail(CombatTaskDetails row, Guid combatTaskId)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.Id == Guid.Empty)
            throw new InvalidOperationException("CombatTaskDetails.Id must be set.");

        if (row.PersonId == Guid.Empty)
            throw new InvalidOperationException("CombatTaskDetails.PersonId must be set.");

        return new CombatTaskDetails
        {
            Id = row.Id,
            CombatTaskId = combatTaskId,
            Kind = row.Kind,
            EffectiveAt = row.EffectiveAt,
            PersonId = row.PersonId,
            Rnokpp = row.Rnokpp,
            FullName = row.FullName,
            Rank = row.Rank,
            Position = row.Position,
            Weapon = row.Weapon,
            Callsign = row.Callsign
        };
    }
}
