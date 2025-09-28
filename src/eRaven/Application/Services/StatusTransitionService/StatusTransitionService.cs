//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.StatusTransitionService;

public sealed class StatusTransitionService(IDbContextFactory<AppDbContext> dbf) : IStatusTransitionService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    public async Task<Dictionary<int, HashSet<int>>> GetAllMapAsync(CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var rows = await db.StatusTransitions
            .AsNoTracking()
            .Select(t => new { t.FromStatusKindId, t.ToStatusKindId })
            .ToListAsync(ct);

        var map = new Dictionary<int, HashSet<int>>();
        foreach (var r in rows)
        {
            if (!map.TryGetValue(r.FromStatusKindId, out var set))
                map[r.FromStatusKindId] = set = [];
            set.Add(r.ToStatusKindId);
        }

        return map;
    }

    public async Task<HashSet<int>> GetToIdsAsync(int fromStatusKindId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var ids = await db.StatusTransitions
            .AsNoTracking()
            .Where(t => t.FromStatusKindId == fromStatusKindId)
            .Select(t => t.ToStatusKindId)
            .Distinct()
            .ToListAsync(ct);

        return [.. ids];
    }

    public async Task SaveAllowedAsync(int fromStatusKindId, IReadOnlyCollection<int> allowedToIds, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        // Забороняємо self-loop на рівні сервісу теж
        var clean = allowedToIds.Where(id => id != fromStatusKindId).ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Текущі
        var current = await db.StatusTransitions
            .Where(t => t.FromStatusKindId == fromStatusKindId)
            .Select(t => t.ToStatusKindId)
            .ToListAsync(ct);

        var currentSet = current.ToHashSet();

        var toAdd = clean.Except(currentSet).ToArray();
        var toRemove = currentSet.Except(clean).ToArray();

        if (toRemove.Length > 0)
        {
            var rows = await db.StatusTransitions
                .Where(t => t.FromStatusKindId == fromStatusKindId && toRemove.Contains(t.ToStatusKindId))
                .ToListAsync(ct);
            db.StatusTransitions.RemoveRange(rows);
        }

        foreach (var to in toAdd)
        {
            db.StatusTransitions.Add(new StatusTransition
            {
                FromStatusKindId = fromStatusKindId,
                ToStatusKindId = to
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
