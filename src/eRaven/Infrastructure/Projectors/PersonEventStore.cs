//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventStore
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Projectors;

public sealed class PersonEventStore(
    IDbContextFactory<AppDbContext> dbFactory,
    PersonEventRecordFactory recordFactory) : IPersonEventStore
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly PersonEventRecordFactory _recordFactory = recordFactory;

    public async Task<bool> PersonExistsAsync(string rnokpp, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PersonRead
            .AsNoTracking()
            .AnyAsync(x => x.Rnokpp == rnokpp, ct);
    }

    public async Task PersistAndProjectAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var records = new List<PersonEventRecord>(events.Count);
        for (var i = 0; i < events.Count; i++)
            records.Add(_recordFactory.Create(events[i], version: i + 1));

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.PersonEvents.AddRange(records);
        await db.SaveChangesAsync(ct);

        var projector = new PersonReadModelProjector(db);
        foreach (var record in records)
            await projector.ProjectAsync(record, ct);

        await tx.CommitAsync(ct);
    }
}
