//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

/// <summary>
/// Репозиторій політик табеля (довідник кодів + дозволені переходи).
///
/// Джерело істини:
/// - <see cref="TimesheetCodeDefinition"/> — список кодів та їх правила завершення.
/// - <see cref="TimesheetCodeTransition"/> — allowed next codes (FromCodeId -> ToCodeId).
///
/// Примітка:
/// - Lane прибрано. Політика застосовується глобально до всіх кодів.
/// </summary>
public sealed class TimesheetPolicyRepository(IDbContextFactory<AppDbContext> dbFactory) : ITimesheetPolicyRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// Повертає всі активні коди табеля, відсортовані для UI.
    /// </summary>
    public async Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Повертає множину дозволених наступних кодів (ToCodeId) для вказаного FromCodeId.
    /// </summary>
    public async Task<IReadOnlySet<Guid>> GetAllowedNextAsync(Guid fromCodeId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var ids = await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == fromCodeId)
            .Select(x => x.ToCodeId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    /// <summary>
    /// Зберігає політику для одного коду:
    /// - оновлює EndDateMeaning / NextCodeOnEnd
    /// - переписує transitions (From->To) відповідно до allowedToCodeIds
    ///
    /// Правила:
    /// - Якщо <paramref name="endDateMeaning"/> == FirstDayOfNextCode:
    ///   - nextCodeOnEnd якщо пустий => дефолт "30"
    ///   - nextCodeOnEnd має існувати серед активних кодів
    /// - Якщо <paramref name="endDateMeaning"/> == LastDayOfThisCode:
    ///   - NextCodeOnEnd завжди стає null
    /// - allowedToCodeIds:
    ///   - ігноруємо Guid.Empty та самого себе
    ///   - всі to-коди мають існувати та бути активними, інакше кидаємо помилку
    /// </summary>
    public async Task SavePolicyAsync(
        Guid fromCodeId,
        TimesheetEndDateMeaning endDateMeaning,
        string? nextCodeOnEnd,
        IReadOnlyCollection<Guid> allowedToCodeIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(author))
            author = "system";

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Load from-code
        var fromCode = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == fromCodeId, ct);

        ArgumentNullException.ThrowIfNull(fromCode, nameof(fromCode));

        // 2) Normalize nextCodeOnEnd (optional)
        nextCodeOnEnd = string.IsNullOrWhiteSpace(nextCodeOnEnd) ? null : nextCodeOnEnd.Trim();

        if (endDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode)
        {
            // default if user didn't pick (historically: "30")
            if (string.IsNullOrWhiteSpace(nextCodeOnEnd))
                nextCodeOnEnd = "30";

            var exists = await db.TimesheetCodes
                .AsNoTracking()
                .AnyAsync(x => x.IsActive && x.Code == nextCodeOnEnd, ct);

            if (!exists)
                throw new InvalidOperationException($"NextCodeOnEnd '{nextCodeOnEnd}' не знайдено.");
        }
        else
        {
            // LastDayOfThisCode => NextCodeOnEnd не має сенсу
            nextCodeOnEnd = null;
        }

        // 3) Update code properties
        fromCode.EndDateMeaning = endDateMeaning;
        fromCode.NextCodeOnEnd = nextCodeOnEnd;
        fromCode.UpdatedBy = author.Trim();
        fromCode.UpdatedAtUtc = nowUtc;

        // 4) Normalize allowedToCodeIds (no empty, no self)
        var toIdsRequested = allowedToCodeIds?
            .Where(id => id != Guid.Empty && id != fromCodeId)
            .Distinct()
            .ToArray() ?? [];

        // 5) Rewrite transitions
        var existing = await db.TimesheetCodeTransitions
            .Where(x => x.FromCodeId == fromCodeId)
            .ToListAsync(ct);

        if (existing.Count > 0)
            db.TimesheetCodeTransitions.RemoveRange(existing);

        if (toIdsRequested.Length > 0)
        {
            var toIdsValid = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.IsActive && toIdsRequested.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (toIdsValid.Count != toIdsRequested.Length)
                throw new InvalidOperationException("Серед дозволених переходів є неіснуючі/неактивні коди.");

            foreach (var toId in toIdsValid)
            {
                db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
                {
                    Id = Guid.NewGuid(),
                    FromCodeId = fromCodeId,
                    ToCodeId = toId,
                    CreatedBy = author.Trim(),
                    CreatedAtUtc = nowUtc
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
