//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries.Personal;
using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Domain.ValueObjects;
using eRaven.Exceptions;
using eRaven.Infrastructure.Projectors;
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

    public async Task<PagedResult<PersonListItemDto>> GetPageAsync(GetPersonsPageQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.PersonRead.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            var sLower = s.ToLowerInvariant();
            var patternLower = $"%{sLower}%";
            var pattern = $"%{s}%";

            q = q.Where(x =>
                // FullName: case-insensitive через LOWER(...)
                EF.Functions.Like(x.FullName.ToLower(), patternLower) ||
                // Rnokpp: цифри, можна без lower
                EF.Functions.Like(x.Rnokpp, pattern));
        }

        if (query.Lifecycle is not null)
            q = q.Where(x => x.Lifecycle == query.Lifecycle);

        if (query.EnrollmentKind is not null)
            q = q.Where(x => x.EnrollmentKind == query.EnrollmentKind);

        if (query.AsOfDate is not null)
        {
            var d = query.AsOfDate.Value;
            // "активний на дату" (як у коментарі індексу):
            // EnrolledAt <= d AND (ExcludedAt IS NULL OR ExcludedAt >= d)
            q = q.Where(x =>
                x.EnrolledAt != null &&
                x.EnrolledAt <= d &&
                (x.ExcludedAt == null || x.ExcludedAt >= d));
        }

        var total = await q.CountAsync(ct);

        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize is < 1 or > 200 ? 25 : query.PageSize;
        var skip = (page - 1) * size;

        var items = await q
            .OrderBy(x => x.PositionSort)
            .Skip(skip)
            .Take(size)
            .Select(x => new PersonListItemDto(
                x.Id,
                x.FullName,
                x.Rnokpp,
                x.Lifecycle,
                x.Rank,
                x.PositionSort,
                x.Position,
                x.EnrollmentKind,
                x.EnrolledAt,
                x.ExcludedAt,
                x.UpdatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<PersonListItemDto>(items, page, size, total);
    }

    public async Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetPersonsSearchAsync(string search, int takePersons, CancellationToken ct = default)
    {
        // TODO need tests
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var take = takePersons <= 0 ? 50 : Math.Min(takePersons, 200);
        var s = (search ?? string.Empty).Trim();

        var q = db.PersonRead.AsNoTracking();

        // Мінімум 2 символи — інакше не вантажимо список (щоб не вбивати UI)
        if (s.Length >= 2)
        {
            q = q.Where(p =>
                (p.FullName != null && EF.Functions.ILike(p.FullName, $"%{s}%")) ||
                (p.Rnokpp != null && EF.Functions.ILike(p.Rnokpp, $"%{s}%")) ||
                (p.Callsign != null && EF.Functions.ILike(p.Callsign, $"%{s}%"))
            );
        }
        else
        {
            // Порожній пошук: повертаємо пусто — юзер має почати вводити
            return [];
        }

        return await q
            .OrderBy(p => p.FullName)
            .Select(p => new CombatTaskPersonLookupDto(
                PersonId: p.Id,
                RNOKPP: p.Rnokpp ?? string.Empty,
                FullName: p.FullName ?? string.Empty,
                Rank: p.Rank ?? string.Empty,
                Position: p.Position ?? string.Empty,
                Weapon: p.Weapon ?? string.Empty,
                Callsign: p.Callsign ?? string.Empty))
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<PersonDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var rm = await db.PersonRead.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rm is null) return null;

        return new PersonDetailsDto(
            Id: rm.Id,
            Lifecycle: rm.Lifecycle,
            EnrollmentKind: rm.EnrollmentKind,
            EnrollmentReference: rm.EnrollmentReference,
            Rnokpp: rm.Rnokpp,
            LastName: rm.LastName,
            FirstName: rm.FirstName,
            MiddleName: rm.MiddleName,
            FullName: rm.FullName,
            Rank: rm.Rank,
            PositionSort: rm.PositionSort,
            Position: rm.Position,
            Bzvp: rm.Bzvp,
            Weapon: rm.Weapon,
            Callsign: rm.Callsign,
            EnrolledAt: rm.EnrolledAt,
            ExcludedAt: rm.ExcludedAt,
            Version: rm.Version,
            UpdatedAtUtc: rm.UpdatedAtUtc
        );
    }

    public async Task<IReadOnlyList<PersonEventDto>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var rows = await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .Select(x => new PersonEventDto(
                x.Version,
                x.EventId,
                x.EventType,
                x.PayloadJson,
                x.Author,
                x.OccurredAtUtc,
                x.EffectiveDate))
            .ToListAsync(ct);

        return rows;
    }

    // =========================
    // Commands aggregate
    // =========================

    public async Task<Guid> CreateReservedAsync(CreateReservedCommand cmd, CancellationToken ct = default)
    {
        var personal = new PersonalInfo(cmd.Rnokpp, cmd.LastName, cmd.FirstName, cmd.MiddleName);

        var agg = PersonAggregate.CreateReserved(
            id: cmd.PersonId,
            personal: personal,
            rank: cmd.Rank,
            position: cmd.Position,
            author: cmd.Author,
            nowUtc: cmd.NowUtc);

        await PersistAsync(agg, expectedVersion: 0, ct);
        return agg.Id;
    }

    public async Task EnrollAsync(EnrollCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);

        agg.Enroll(
             kind: cmd.Kind,
             reference: cmd.Reference,
             reason: cmd.Reason,
             enrollDate: cmd.EnrollDate,
             rank: cmd.Rank,
             position: cmd.Position,
             positionSort: cmd.PositionSort,
             author: cmd.Author,
             nowUtc: cmd.NowUtc);

        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ExcludeAsync(ExcludeCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.Exclude(cmd.Reason, cmd.EffectiveDate, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    // =========================
    // Commands personal info
    // =========================

    public async Task UpdatePersonalInfoAsync(UpdatePersonalInfoCommand cmd, CancellationToken ct = default)
    {
        var personal = new PersonalInfo(cmd.Rnokpp, cmd.LastName, cmd.FirstName, cmd.MiddleName);

        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.UpdatePersonalInfo(personal, cmd.Note, cmd.Author, cmd.NowUtc);

        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ChangeRankAsync(ChangeRankCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.ChangeRank(cmd.EffectiveDate, cmd.Rank, cmd.Note, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ChangePositionAsync(ChangePositionCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.ChangePosition(cmd.EffectiveDate, cmd.PositionSort ?? 0, cmd.Position ?? string.Empty, cmd.Note, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ChangeBzvpAsync(ChangeBzvpCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.ChangeBzvp(cmd.EffectiveDate, cmd.Bzvp, cmd.Note, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ChangeWeaponAsync(ChangeWeaponCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.ChangeWeapon(cmd.EffectiveDate, cmd.Weapon, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }

    public async Task ChangeCallsignAsync(ChangeCallsignCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.ChangeCallsign(cmd.EffectiveDate, cmd.Callsign, cmd.Author, cmd.NowUtc);
        await PersistAsync(agg, expectedVersion: agg.Version, ct);
    }



    public async Task VoidEventAsync(VoidPersonEventCommand cmd, CancellationToken ct = default)
    {
        var agg = await LoadAggregateAsync(cmd.PersonId, ct);
        agg.VoidEvent(cmd.TargetEventId, cmd.Reason, cmd.Author, cmd.NowUtc);
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

    public async Task<IReadOnlySet<string>> GetExistingRnokppsAsync(
    IReadOnlyCollection<string> rnokpps,
    CancellationToken ct = default)
    {
        var set = rnokpps?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        if (set.Length == 0)
            return new HashSet<string>(StringComparer.Ordinal);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.PersonRead
            .AsNoTracking()
            .Where(x => set.Contains(x.Rnokpp))
            .Select(x => x.Rnokpp)
            .ToListAsync(ct);

        return new HashSet<string>(existing, StringComparer.Ordinal);
    }

    public async Task<Guid> BootstrapCreateAndEnrollAsync(
        PersonBootstrapRowDto row,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        // мінімальна страховка на бекові інваріанти
        if (string.IsNullOrWhiteSpace(row.Rnokpp)) throw new ArgumentException("Rnokpp is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) throw new ArgumentException("LastName is required.");
        if (string.IsNullOrWhiteSpace(row.FirstName)) throw new ArgumentException("FirstName is required.");
        if (string.IsNullOrWhiteSpace(row.Rank)) throw new ArgumentException("Rank is required.");
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
