//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Exceptions;
using eRaven.Infrastructure.Projectors;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public sealed class PersonRepository(
    IDbContextFactory<AppDbContext> dbFactory,
    IPersonReadModelProjector projector)
    : IPersonRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly IPersonReadModelProjector _projector = projector;

    public async Task<PersonAggregate?> LoadAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var records = await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
            return null;

        var stored = records
            .Select(r => new PersonAggregate.StoredEvent(
                Version: r.Version,
                Event: PersonEventTypeRegistry.Deserialize(r)))
            .ToList();

        var agg = new PersonAggregate();
        agg.LoadFromHistory(stored);
        return agg;
    }

    public async Task SaveAsync(PersonAggregate agg, long expectedVersion, CancellationToken ct = default)
    {
        if (agg.Id == Guid.Empty)
            throw new InvalidOperationException("Aggregate must be initialized before saving.");

        var changes = agg.GetUncommittedChanges();
        if (changes.Count == 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) optimistic concurrency: текущая версия стрима в БД
        var currentVersion = await db.PersonEvents
            .Where(x => x.AggregateId == agg.Id)
            .MaxAsync(x => (long?)x.Version, ct) ?? 0;

        if (currentVersion != expectedVersion)
            throw new ConcurrencyException(
                $"Concurrency conflict for {agg.Id}. ExpectedVersion={expectedVersion}, CurrentVersion={currentVersion}");

        // 2) append: expectedVersion + 1..N
        var records = new List<PersonEventRecord>(changes.Count);

        for (int i = 0; i < changes.Count; i++)
        {
            var version = expectedVersion + i + 1;
            records.Add(ToRecord(changes[i], agg.Id, version));
        }

        db.PersonEvents.AddRange(records);
        await db.SaveChangesAsync(ct);

        // 3) sync projection В ТОЙ ЖЕ ТРАНЗАКЦИИ и на том же DbContext
        foreach (var r in records)
            await _projector.ProjectAsync(db, r, ct);

        // ✅ Если хочешь быстрее: убери SaveChanges из ProjectAsync и сделай один финальный SaveChanges здесь.

        await tx.CommitAsync(ct);

        // 4) update aggregate stream version + clear
        agg.MarkChangesAsCommitted(expectedVersion + records.Count);
    }

    private static PersonEventRecord ToRecord(IDomainEvent evt, Guid aggregateId, long version)
    {
        var eventType = evt.GetType().Name;

        var payloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);

        // Подстрой под твою сущность PersonEventRecord
        return new PersonEventRecord
        {
            AggregateId = aggregateId,
            Version = version,

            EventId = evt.EventId,
            EventType = eventType,

            OccurredAtUtc = evt.OccurredAtUtc,
            PayloadJson = payloadJson
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}