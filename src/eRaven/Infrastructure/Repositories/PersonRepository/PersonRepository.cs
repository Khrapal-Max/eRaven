//-----------------------------------------------------------------------------
// All rights by agreement of the developer. author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Person;
using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Domain.ValueObjects;
using eRaven.Exceptions;
using eRaven.Infrastructure.Projections.Person;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public sealed class PersonRepository(
    IDbContextFactory<AppDbContext> dbFactory,
    IPersonReadModelProjector projector) : IPersonRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // =========================
    // JSON
    // =========================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string SerializeEvent(IDomainEvent evt)
        => JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);

    private static DateOnly? ExtractEffectiveDate(IDomainEvent evt) => evt switch
    {
        PersonRankChanged x => x.EffectiveDate,
        PersonPositionChanged x => x.EffectiveDate,
        PersonBzvpChanged x => x.EffectiveDate,
        PersonWeaponChanged x => x.EffectiveDate,
        PersonCallsignChanged x => x.EffectiveDate,
        PersonEnrolled x => x.EnrollDate,
        PersonExcluded x => x.EffectiveDate,
        _ => null
    };

    // =========================
    // Read-side
    // =========================

    /// <inheritdoc />
    public async Task<PagedResult<PersonReadModel>> GetPageAsync(
        int page,
        int pageSize,
        string? search = null,
        DateOnly? asOfDate = null,
        PersonLifecycle? lifecycle = null,
        EnrollmentKind? enrollmentKind = null,
        CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var size = pageSize is < 1 or > 200 ? 25 : pageSize;
        var skip = (pageNumber - 1) * size;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<PersonReadModel> q = db.PersonRead.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var sLower = s.ToLowerInvariant();
            var patternLower = $"%{sLower}%";
            var pattern = $"%{s}%";

            q = q.Where(x =>
                EF.Functions.Like(x.FullName.ToLower(), patternLower) ||
                EF.Functions.Like(x.Rnokpp, pattern));
        }

        if (lifecycle is not null)
            q = q.Where(x => x.Lifecycle == lifecycle);

        if (enrollmentKind is not null)
            q = q.Where(x => x.EnrollmentKind == enrollmentKind);

        if (asOfDate is not null)
        {
            var d = asOfDate.Value;

            q = q.Where(x =>
                x.EnrolledAt != null &&
                x.EnrolledAt <= d &&
                (x.ExcludedAt == null || x.ExcludedAt >= d));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(x => x.PositionSort)
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<PersonReadModel>(items, pageNumber, size, total);
    }

    /// <inheritdoc />
    public async Task<PersonReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Person ID is required.", nameof(id));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PersonRead
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonReadModel>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PersonRead
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.PositionSort)
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonEventRecord>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Person ID is required.", nameof(id));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .ToListAsync(ct);
    }

    // =========================
    // Commands aggregate
    // =========================

    /// <inheritdoc />
    public async Task<Guid> CreateReservedAsync(string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string? rank,
        string? position,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var personal = new PersonalInfo(rnokpp, lastName, firstName, middleName);

        var agg = PersonAggregate.CreateReserved(
            id: Guid.NewGuid(),
            personal: personal,
            rank: rank,
            position: position,
            author: author,
            nowUtc: nowUtc);

        await PersistAsync(agg, expectedVersion: 0, ct);
        return agg.Id;
    }

    /// <inheritdoc />
    public async Task EnrollAsync(Guid personId,
        EnrollmentKind kind,
        string? reference,
        string reason,
        DateOnly enrollDate,
        string rank,
        int positionSort,
        string position,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);

        agg.Enroll(
             kind: kind,
             reference: reference,
             reason: reason,
             enrollDate: enrollDate,
             rank: rank,
             position: position,
             positionSort: positionSort,
             author: author,
             nowUtc: nowUtc);

        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ExcludeAsync(Guid personId,
        string reason,
        DateOnly effectiveDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.Exclude(reason, effectiveDate, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    // =========================
    // Commands personal info
    // =========================

    /// <inheritdoc />
    public async Task UpdatePersonalInfoAsync(Guid personId,
        string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string? Note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var personal = new PersonalInfo(rnokpp, lastName, firstName, middleName);

        var agg = await LoadAggregateAsync(personId, ct);
        agg.UpdatePersonalInfo(personal, Note, author, nowUtc);

        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ChangeRankAsync(Guid personId,
        DateOnly effectiveDate,
        string rank,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.ChangeRank(effectiveDate, rank, note, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ChangePositionAsync(Guid personId,
        DateOnly effectiveDate,
        int? positionSort,
        string? position,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.ChangePosition(effectiveDate, positionSort ?? 0, position ?? string.Empty, note, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ChangeBzvpAsync(Guid personId,
        DateOnly effectiveDate,
        string bzvp,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.ChangeBzvp(effectiveDate, bzvp, note, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ChangeWeaponAsync(Guid personId,
        DateOnly effectiveDate,
        string? weapon,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.ChangeWeapon(effectiveDate, weapon, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task ChangeCallsignAsync(Guid personId,
        DateOnly effectiveDate,
        string? callsign,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.ChangeCallsign(effectiveDate, callsign, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    /// <inheritdoc />
    public async Task VoidEventAsync(Guid personId,
        Guid targetEventId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(personId, ct);
        agg.VoidEvent(targetEventId, reason, author, nowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    // =========================
    // Aggregate load / persist
    // =========================

    private async Task<PersonAggregate> LoadAggregateAsync(Guid id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var records = await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .ThenBy(x => x.EventId)
            .ToListAsync(ct);

        if (records.Count == 0)
            throw new InvalidOperationException($"Person stream not found: {id}");

        var history = records
            .Select(r => new PersonAggregate.StoredEvent(
                r.Version,
                PersonEventTypeRegistry.Deserialize(r)))
            .ToList();

        var agg = new PersonAggregate();
        agg.LoadFromHistory(history);

        return agg;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetExistingRnokppsAsync(IReadOnlyCollection<string> rnokpps,
        CancellationToken ct = default)
    {
        var set = rnokpps?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        if (set.Length == 0)
            return new HashSet<string>(StringComparer.Ordinal);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.PersonRead
            .AsNoTracking()
            .Where(x => set.Contains(x.Rnokpp))
            .Select(x => x.Rnokpp)
            .ToListAsync(ct);

        return new HashSet<string>(existing, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<Guid> BootstrapCreateAndEnrollAsync(
        PersonBootstrapRowDto row,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        // мінімальна страховка на бекові інваріанти
        if (string.IsNullOrWhiteSpace(row.Rnokpp)) throw new ArgumentException("rnokpp is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) throw new ArgumentException("lastName is required.");
        if (string.IsNullOrWhiteSpace(row.FirstName)) throw new ArgumentException("firstName is required.");
        if (string.IsNullOrWhiteSpace(row.Rank)) throw new ArgumentException("rank is required.");
        if (string.IsNullOrWhiteSpace(row.Position)) throw new ArgumentException("Position is required.");
        if (string.IsNullOrWhiteSpace(row.Reason)) throw new ArgumentException("Reason is required.");

        // rule: non-Unit => 9999
        var positionSort = row.Kind == EnrollmentKind.Unit
            ? row.PositionSort
            : 9999;

        var id = Guid.NewGuid();

        var personal = new PersonalInfo(
            rnokpp: row.Rnokpp.Trim(),
            lastName: row.LastName.Trim(),
            firstName: row.FirstName.Trim(),
            middleName: string.IsNullOrWhiteSpace(row.MiddleName) ? null : row.MiddleName.Trim());

        // 1 агрегат = 1 PersistAsync (атомарно для картки)
        var agg = PersonAggregate.CreateReserved(
            id: id,
            personal: personal,
            rank: row.Rank.Trim(),
            position: row.Position.Trim(),
            author: author,
            nowUtc: nowUtc);

        agg.Enroll(
            kind: row.Kind,
            reference: string.IsNullOrWhiteSpace(row.Reference) ? null : row.Reference.Trim(),
            reason: row.Reason.Trim(),
            enrollDate: row.EnrollDate,
            rank: row.Rank.Trim(),
            positionSort: positionSort,
            position: row.Position.Trim(),
            author: author,
            nowUtc: nowUtc);

        // optional fields -> effective date = EnrollDate
        if (!string.IsNullOrWhiteSpace(row.Bzvp))
            agg.ChangeBzvp(row.EnrollDate, row.Bzvp.Trim(), note: null, author: author, nowUtc: nowUtc);

        if (!string.IsNullOrWhiteSpace(row.Weapon))
            agg.ChangeWeapon(row.EnrollDate, row.Weapon.Trim(), author: author, nowUtc: nowUtc);

        if (!string.IsNullOrWhiteSpace(row.Callsign))
            agg.ChangeCallsign(row.EnrollDate, row.Callsign.Trim(), author: author, nowUtc: nowUtc);

        await PersistAsync(agg, expectedVersion: 0, ct);
        return id;
    }

    private async Task PersistAsync(PersonAggregate agg, long expectedVersion, CancellationToken ct)
    {
        var changes = agg.GetUncommittedChanges();
        if (changes.Count == 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // optimistic concurrency: last persisted version must equal expectedVersion
        var last = await db.PersonEvents
            .Where(x => x.AggregateId == agg.Id)
            .OrderByDescending(x => x.Version)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(ct);

        var currentVersion = last ?? 0L;
        if (currentVersion != expectedVersion)
            throw new OptimisticConcurrencyException(agg.Id, expectedVersion, currentVersion);

        // 1) append to event store (assign versions)
        var nextVersion = currentVersion;
        var newRecords = new List<PersonEventRecord>(changes.Count);

        foreach (var evt in changes)
        {
            nextVersion++;

            var rec = new PersonEventRecord
            {
                EventId = evt.EventId,
                AggregateId = evt.AggregateId,
                Version = nextVersion,
                EventType = evt.GetType().Name,
                PayloadJson = SerializeEvent(evt),
                Author = evt.Author,
                OccurredAtUtc = evt.OccurredAtUtc,
                EffectiveDate = ExtractEffectiveDate(evt)
            };

            newRecords.Add(rec);
            db.PersonEvents.Add(rec);
        }

        // IMPORTANT: persist events first (so void rebuild can see them in db.PersonEvents)
        await db.SaveChangesAsync(ct);

        // 2) project read model (incremental)
        foreach (var rec in newRecords.OrderBy(x => x.Version))
            await projector.ProjectAsync(db, rec, ct);

        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        agg.MarkChangesAsCommitted(nextVersion);
    }
}
