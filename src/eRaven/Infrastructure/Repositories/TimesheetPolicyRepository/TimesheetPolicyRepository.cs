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

public sealed class TimesheetPolicyRepository(IDbContextFactory<AppDbContext> dbFactory) : ITimesheetPolicyRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(
        TimesheetLane lane,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Lane == lane && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlySet<Guid>> GetAllowedNextAsync(
        Guid fromCodeId,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var ids = await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Where(x => x.FromCodeId == fromCodeId)
            .Select(x => x.ToCodeId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task SavePolicyAsync(
        TimesheetLane lane,
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

        // 1) Load from-code and validate lane
        var fromCode = await db.TimesheetCodes
            .FirstOrDefaultAsync(x => x.Id == fromCodeId, ct);

        ArgumentNullException.ThrowIfNull(fromCode, nameof(fromCode));

        if (fromCode.Lane != lane)
            throw new InvalidOperationException("Lane не відповідає вибраному коду.");

        // 2) Normalize nextCodeOnEnd (optional)
        nextCodeOnEnd = string.IsNullOrWhiteSpace(nextCodeOnEnd) ? null : nextCodeOnEnd.Trim();

        // Якщо meaning = FirstDayOfNextCode, бажано щоб NextCodeOnEnd існував (для Main).
        if (endDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode)
        {
            if (lane == TimesheetLane.Main)
            {
                if (string.IsNullOrWhiteSpace(nextCodeOnEnd))
                    nextCodeOnEnd = "30";

                var exists = await db.TimesheetCodes
                    .AsNoTracking()
                    .AnyAsync(x => x.Lane == lane && x.IsActive && x.Code == nextCodeOnEnd, ct);

                if (!exists)
                    throw new InvalidOperationException($"NextCodeOnEnd '{nextCodeOnEnd}' не знайдено в lane {lane}.");
            }
            else
            {
                // Для Task зазвичай це не потрібно
                nextCodeOnEnd = null;
            }
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

        // 4) Validate allowedToCodeIds: must exist, must be same lane, must be active, must not include itself
        var toIdsRequested = allowedToCodeIds?
            .Where(id => id != Guid.Empty && id != fromCodeId)
            .Distinct()
            .ToArray() ?? [];

        if (toIdsRequested.Length > 0)
        {
            var toIdsValid = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.Lane == lane && x.IsActive && toIdsRequested.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            // Якщо хтось передав "чужі" або неіснуючі — не мовчимо, кидаємо помилку
            if (toIdsValid.Count != toIdsRequested.Length)
                throw new InvalidOperationException("Серед дозволених переходів є коди з іншого lane або неіснуючі/неактивні.");

            // 5) Rewrite transitions for fromCodeId
            var existing = await db.TimesheetCodeTransitions
                .Where(x => x.FromCodeId == fromCodeId)
                .ToListAsync(ct);

            db.TimesheetCodeTransitions.RemoveRange(existing);

            foreach (var toId in toIdsValid)
            {
                db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
                {
                    Id = Guid.NewGuid(),
                    Lane = lane,
                    FromCodeId = fromCodeId,
                    ToCodeId = toId,
                    CreatedBy = author.Trim(),
                    CreatedAtUtc = nowUtc
                });
            }
        }
        else
        {
            // якщо пусто — просто очищаємо transitions
            var existing = await db.TimesheetCodeTransitions
                .Where(x => x.FromCodeId == fromCodeId)
                .ToListAsync(ct);

            db.TimesheetCodeTransitions.RemoveRange(existing);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
