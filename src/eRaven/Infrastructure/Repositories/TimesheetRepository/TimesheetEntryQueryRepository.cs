//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryQueryRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-only EF Core repository for <see cref="TimesheetEntry"/>.
///
/// <para>
/// Інтервали: <c>[From..To)</c>, <c>To</c> — <b>exclusive</b>.
/// Soft-deleted (IsDeleted=true) ігноруються.
/// </para>
/// </summary>
public sealed class TimesheetEntryQueryRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetEntryQueryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Range queries
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(
        IReadOnlyCollection<Guid> personIds,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken ct = default)
    {
        if (personIds.Count == 0) return Array.Empty<TimesheetEntry>();
        if (from == default) throw new ArgumentException("from is required.", nameof(from));
        if (toExclusive == default) throw new ArgumentException("toExclusive is required.", nameof(toExclusive));
        if (toExclusive <= from) return Array.Empty<TimesheetEntry>();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => personIds.Contains(x.PersonId))
            // overlap with [from..toExclusive)
            .Where(x => x.From < toExclusive && (!x.To.HasValue || x.To.Value > from))
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.From)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonAsync(
        Guid personId,
        DateOnly from,
        DateOnly toExclusive,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));
        if (from == default) throw new ArgumentException("from is required.", nameof(from));
        if (toExclusive == default) throw new ArgumentException("toExclusive is required.", nameof(toExclusive));
        if (toExclusive <= from) return Array.Empty<TimesheetEntry>();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From < toExclusive && (!x.To.HasValue || x.To.Value > from))
            .OrderBy(x => x.From)
            .ToListAsync(ct);
    }

    //======================================================================
    // Point queries
    //======================================================================

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetByIdAsync(Guid entryId, CancellationToken ct = default)
    {
        if (entryId == Guid.Empty) throw new ArgumentException("entryId is required.", nameof(entryId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .FirstOrDefaultAsync(x => x.Id == entryId, ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetActiveEntryOnDateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));
        if (date == default) throw new ArgumentException("date is required.", nameof(date));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var timesheetId = await ResolveEpisodeIdOnDateAsync(db, personId, date, ct);
        if (timesheetId == Guid.Empty)
            return null;

        return await db.TimesheetEntries
            .AsNoTracking()
            .Include(x => x.TimesheetCodeDefinition)
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timesheetId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= date && (!x.To.HasValue || date < x.To.Value))
            .OrderByDescending(x => x.From)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetNextEntryAfterDateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));
        if (date == default) throw new ArgumentException("date is required.", nameof(date));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var timesheetId = await ResolveEpisodeIdOnDateAsync(db, personId, date, ct);
        if (timesheetId == Guid.Empty)
            return null;

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timesheetId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From > date)
            .OrderBy(x => x.From)
            .FirstOrDefaultAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Повертає Id епізоду, який покриває дату.
    /// <para>
    /// Якщо дані пошкоджені (декілька епізодів накладаються) — метод кине виняток (SingleOrDefault).
    /// </para>
    /// </summary>
    private static async Task<Guid> ResolveEpisodeIdOnDateAsync(
        AppDbContext db,
        Guid personId,
        DateOnly date,
        CancellationToken ct)
    {
        return await db.TimeSheets
            .AsNoTracking()
            .Where(t => t.PersonId == personId)
            .Where(t => t.OpenedAt <= date && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= date))
            .Select(t => t.Id)
            .SingleOrDefaultAsync(ct);
    }
}
