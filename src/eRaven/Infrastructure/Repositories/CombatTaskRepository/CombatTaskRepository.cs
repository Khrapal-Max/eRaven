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
/// CRUD-репозиторій бойових завдань у межах документа:
/// - <see cref="CombatTask"/> (групування по місії),
/// - <see cref="CombatTaskDetails"/> (рядки Start/End по особам).
///
/// Важливо:
/// - Бізнес-правила (Draft/Posted/Canceled, конфлікти, apply в MissionAssignment/Timesheet)
///   мають бути в handler’ах.
/// - Тут лишаємо лише цілісність даних і коректні зв'язки.
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

        var tasks = await db.CombatTasks
            .AsNoTracking()
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
    public async Task<Guid> CreateCombatTask(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        ICollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (missionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(missionId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var docExists = await db.CombatTaskDocuments
            .AsNoTracking()
            .AnyAsync(x => x.Id == documentId, ct);

        if (!docExists)
            throw new InvalidOperationException("Документ не знайдено.");

        ValidateDetails(combatTaskDetails);

        var task = new CombatTask
        {
            Id = Guid.NewGuid(),
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            SourceDocument = sourceDocument?.Trim() ?? string.Empty
        };

        // Важливо: спочатку додаємо task, щоб FK був валідний в межах SaveChanges.
        db.CombatTasks.Add(task);

        PrepareLinesForInsert(task.Id, combatTaskDetails);
        db.CombatTaskDetails.AddRange(combatTaskDetails);

        await db.SaveChangesAsync(ct);
        return task.Id;
    }

    //======================================================================
    // Write: Delete
    //======================================================================

    /// <inheritdoc />
    public async Task DeleteCombatTask(Guid documentId, Guid combatTaskId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (combatTaskId == Guid.Empty)
            throw new ArgumentException("CombatTaskId is required.", nameof(combatTaskId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

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
    /// Приводить lines до інваріанту перед insert:
    /// - гарантує Id,
    /// - проставляє FK CombatTaskId,
    /// - чистить navigation, щоб EF не тягнув зайвий граф.
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
    /// - PersonId / EffectiveAt,
    /// - required snapshot поля (Rnokpp/FullName),
    /// - дублікати в межах одного CombatTask: (PersonId, Kind, EffectiveAt).
    /// </summary>
    private static void ValidateDetails(ICollection<CombatTaskDetails> lines)
    {
        ArgumentNullException.ThrowIfNull(lines, nameof(lines));

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
                throw new InvalidOperationException(
                    $"Дублікат рядка: PersonId={x.PersonId}, Kind={x.Kind}, EffectiveAt={x.EffectiveAt:yyyy-MM-dd}.");
        }
    }
}
