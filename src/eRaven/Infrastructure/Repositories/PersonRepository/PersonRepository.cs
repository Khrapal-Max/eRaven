//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Events;
using eRaven.Exceptions;
using eRaven.Infrastructure.Projectors;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public async Task<long> SaveAsync(PersonAggregate agg, long expectedVersion, CancellationToken ct = default)
    {
        if (agg.Id == Guid.Empty)
            throw new InvalidOperationException("Aggregate must be initialized before saving.");

        var changes = agg.GetUncommittedChanges();
        if (changes.Count == 0)
            return expectedVersion;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        var newVersion = await SaveAsync(db, agg, expectedVersion, ct);

        await tx.CommitAsync(ct);

        // Коммит завершён — можно фиксировать версию агрегата и чистить uncommitted
        agg.MarkChangesAsCommitted(newVersion);
        return newVersion;
    }

    public async Task<long> SaveAsync(AppDbContext db, PersonAggregate agg, long expectedVersion, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (agg.Id == Guid.Empty)
            throw new InvalidOperationException("Aggregate must be initialized before saving.");

        var changes = agg.GetUncommittedChanges();
        if (changes.Count == 0)
            return expectedVersion;

        // 1) optimistic concurrency: текущая версия в БД
        var currentVersion = await db.PersonEvents
            .Where(x => x.AggregateId == agg.Id)
            .MaxAsync(x => (long?)x.Version, ct) ?? 0;

        if (currentVersion != expectedVersion)
            throw new ConcurrencyException(
                $"Concurrency conflict for {agg.Id}. ExpectedVersion={expectedVersion}, CurrentVersion={currentVersion}");

        // 2) append: expectedVersion+1..N
        var records = new List<PersonEventRecord>(changes.Count);
        for (int i = 0; i < changes.Count; i++)
        {
            var version = expectedVersion + i + 1;
            records.Add(ToRecord(changes[i], agg.Id, version));
        }

        db.PersonEvents.AddRange(records);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueAggregateVersionViolation(ex))
        {
            // На случай гонки: UNIQUE(AggregateId, Version)
            throw new ConcurrencyException(
                $"Concurrency conflict for {agg.Id}. ExpectedVersion={expectedVersion} (unique violation AggregateId+Version).");
        }

        // 3) sync projection на том же DbContext/Tx
        foreach (var r in records)
            await _projector.ProjectAsync(db, r, ct);

        // ВАЖНО:
        // Здесь НЕ вызываем MarkChangesAsCommitted() — пусть хендлер делает это ПОСЛЕ Commit(),
        // иначе при rollback у тебя в памяти будут очищены события.
        return expectedVersion + records.Count;
    }

    private static PersonEventRecord ToRecord(IDomainEvent evt, Guid aggregateId, long version)
    {
        var payloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);

        return new PersonEventRecord
        {
            AggregateId = aggregateId,
            Version = version,

            EventId = evt.EventId,
            EventType = evt.GetType().Name,

            Author = evt.Author,
            OccurredAtUtc = evt.OccurredAtUtc,
            EffectiveDate = GetEffectiveDate(evt),

            PayloadJson = payloadJson
        };
    }

    private static DateOnly? GetEffectiveDate(IDomainEvent evt) => evt switch
    {
        PersonRankChanged x => x.EffectiveDate,
        PersonPositionChanged x => x.EffectiveDate,
        PersonTemporaryPositionChanged x => x.EffectiveDate,
        PersonBzvpChanged x => x.EffectiveDate,
        PersonWeaponChanged x => x.EffectiveDate,
        PersonCallsignChanged x => x.EffectiveDate,
        PersonEnrolled x => x.EnrollDate,
        PersonExcluded x => x.EffectiveDate,
        _ => null
    };

    private static bool IsUniqueAggregateVersionViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}