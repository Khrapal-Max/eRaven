//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents;
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
            .Select(r => new PersonAggregate.StoredEvent(r.Version, PersonEventTypeRegistry.Deserialize(r)))
            .ToList();

        var agg = new PersonAggregate();
        agg.LoadFromHistory(stored);
        return agg;
    }

    public async Task<PagedResult<PersonTableDto>> GetPersonsPageAsync(GetPersonsPageQuery q, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(q.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(q.PageSize, 1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<PersonReadModel> query = db.PersonRead.AsNoTracking();

        // Search (ПІБ + РНОКПП)
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();

            if (db.Database.IsNpgsql())
            {
                // PostgreSQL: ILIKE (case-insensitive, нормально для кирилиці теж)
                query = query.Where(x =>
                    EF.Functions.ILike(x.FullName, $"%{s}%") ||
                    EF.Functions.ILike(x.Rnokpp, $"%{s}%"));
            }
            else
            {
                // SQLite/інші: lower() + LIKE
                // (для тестів цього зазвичай достатньо; у SQLite case-insensitive повноцінно не завжди працює для кирилиці)
                var ss = s.ToLowerInvariant();
                var pattern = $"%{ss}%";

                query = query.Where(x =>
                    EF.Functions.Like(x.FullName.ToLower(), pattern) ||
                    EF.Functions.Like(x.Rnokpp.ToLower(), pattern));
            }
        }

        // Filters
        if (q.EnrollmentKind is not null)
            query = query.Where(x => x.EnrollmentKind == q.EnrollmentKind.Value);

        if (q.Lifecycle is not null)
            query = query.Where(x => x.Lifecycle == q.Lifecycle.Value);

        // AsOfDate (простий “на виріст” варіант на основі EnrolledAt/ExcludedAt)
        // Ідея: визначити "стан на дату" через кордони.
        if (q.AsOfDate is DateOnly asOf)
        {
            query = query.Where(x =>
                // якщо виключений до/на дату — він "не активний" на цю дату
                !(x.ExcludedAt.HasValue && x.ExcludedAt.Value <= asOf));
            // (за потреби можна буде допиляти до “на дату був зарахований” і т.д.)
        }

        var total = await query.CountAsync(ct);

        // Стабільне сортування (можеш замінити під UX)
        query = query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.FullName);

        var skip = (q.Page - 1) * q.PageSize;

        var items = await query
            .Skip(skip)
            .Take(q.PageSize)
            .Select(x => new PersonTableDto(
                x.Id,
                x.FullName,
                x.Rnokpp,
                x.Lifecycle,
                x.EnrollmentKind,
                x.Rank,
                x.Position,
                x.TemporaryPosition,
                x.PlannedPosition,
                x.EnrolledAt,
                x.ExcludedAt,
                x.UpdatedAtUtc
            ))
            .ToListAsync(ct);

        return new PagedResult<PersonTableDto>(items, q.Page, q.PageSize, total);
    }

    public async Task<PersonDto?> GetPersonCardAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var x = await db.PersonRead
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (x is null)
            return null;

        return new PersonDto(
            Id: x.Id,
            FullName: x.FullName,
            Rnokpp: x.Rnokpp,
            Lifecycle: x.Lifecycle,
            EnrollmentKind: x.EnrollmentKind, // має бути nullable в RM
            Rank: x.Rank,
            Position: x.Position,
            TemporaryPosition: x.TemporaryPosition,
            PlannedPosition: x.PlannedPosition,
            EnrolledAt: x.EnrolledAt,
            ExcludedAt: x.ExcludedAt,
            UpdatedAtUtc: x.UpdatedAtUtc,
            Bzvp: x.Bzvp,
            Weapon: x.Weapon,
            Callsign: x.Callsign
        );
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

        try
        {
            // 0) optimistic concurrency: поточна версія стріму в БД
            var currentVersion = await db.PersonEvents
                .Where(x => x.AggregateId == agg.Id)
                .MaxAsync(x => (long?)x.Version, ct) ?? 0;

            if (currentVersion != expectedVersion)
                throw new ConcurrencyException($"Expected={expectedVersion}, Current={currentVersion}");

            // 0.1) snapshot "до" (для правильних переходів станів посад)
            var before = await db.PersonRead
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == agg.Id, ct);

            Guid? reserved = before?.PlannedPositionUnitId;
            Guid? main = before?.PositionUnitId;
            Guid? temp = before?.TemporaryPositionUnitId;

            // 1) застосувати переходи PositionUnit.State відповідно до доменних подій
            //    (робимо до projection; все в тій же транзакції)
            foreach (var evt in changes)
            {
                switch (evt)
                {
                    case PersonCandidateCreated c:
                        {
                            if (c.PlannedPositionUnitId is Guid posId)
                            {
                                await EnsureStateAsync(db, posId,
                                    from: PositionUnitState.Vacant,
                                    to: PositionUnitState.TemporarilyCandidate,
                                    ct);

                                reserved = posId;
                            }
                            break;
                        }

                    case PersonEnrolled e:
                        {
                            // якщо був резерв - або займаємо його, або звільняємо старий і займаємо новий
                            if (reserved is Guid r && r == e.PositionUnitId)
                            {
                                await EnsureStateAsync(db, r,
                                    from: PositionUnitState.TemporarilyCandidate,
                                    to: PositionUnitState.Occupied,
                                    ct);
                            }
                            else
                            {
                                if (reserved is Guid oldReserve)
                                {
                                    await EnsureStateAsync(db, oldReserve,
                                        from: PositionUnitState.TemporarilyCandidate,
                                        to: PositionUnitState.Vacant,
                                        ct);
                                }

                                await EnsureStateAsync(db, e.PositionUnitId,
                                    from: PositionUnitState.Vacant,
                                    to: PositionUnitState.Occupied,
                                    ct);
                            }

                            reserved = null;
                            main = e.PositionUnitId;
                            break;
                        }

                    case PersonExcluded:
                        {
                            // звільнити основну
                            if (main is Guid m)
                            {
                                await EnsureStateAsync(db, m,
                                    from: PositionUnitState.Occupied,
                                    to: PositionUnitState.Vacant,
                                    ct);
                            }

                            // звільнити тимчасову
                            if (temp is Guid tpos)
                            {
                                await EnsureStateAsync(db, tpos,
                                    from: PositionUnitState.TemporarilyOccupied,
                                    to: PositionUnitState.Vacant,
                                    ct);
                            }

                            // звільнити резерв кандидата
                            if (reserved is Guid rpos)
                            {
                                await EnsureStateAsync(db, rpos,
                                    from: PositionUnitState.TemporarilyCandidate,
                                    to: PositionUnitState.Vacant,
                                    ct);
                            }

                            main = null;
                            temp = null;
                            reserved = null;
                            break;
                        }

                    case PersonPositionChanged p:
                        {
                            // main = текущая основная до изменения (ты её держишь из before.PersonRead)
                            if (main is Guid oldMain && oldMain != p.PositionUnitId)
                            {
                                await EnsureStateAsync(db, oldMain,
                                    from: PositionUnitState.Occupied,
                                    to: PositionUnitState.Vacant,
                                    ct);
                            }

                            await EnsureStateAsync(db, p.PositionUnitId,
                                from: PositionUnitState.Vacant,
                                to: PositionUnitState.Occupied,
                                ct);

                            main = p.PositionUnitId;
                            break;
                        }

                    case PersonTemporaryPositionChanged t:
                        {
                            var newTemp = t.TemporaryPositionUnitId;

                            // зняти стару тимчасову, якщо міняємо/скидаємо
                            if (temp is Guid oldTemp && oldTemp != newTemp)
                            {
                                await EnsureStateAsync(db, oldTemp,
                                    from: PositionUnitState.TemporarilyOccupied,
                                    to: PositionUnitState.Vacant,
                                    ct);
                            }

                            // поставити нову тимчасову
                            if (newTemp is Guid nt && nt != temp)
                            {
                                await EnsureStateAsync(db, nt,
                                    from: PositionUnitState.Vacant,
                                    to: PositionUnitState.TemporarilyOccupied,
                                    ct);
                            }

                            temp = newTemp;
                            break;
                        }
                }
            }

            // 2) append events expectedVersion+1..N
            var records = new List<PersonEventRecord>(changes.Count);
            for (int i = 0; i < changes.Count; i++)
            {
                var version = expectedVersion + i + 1;
                records.Add(ToRecord(changes[i], agg.Id, version));
            }

            db.PersonEvents.AddRange(records);

            // важливо: зберігаємо евенти, щоб void/rebuild бачив історію
            await db.SaveChangesAsync(ct);

            // 3) sync projection (Projector БЕЗ SaveChanges всередині!)
            foreach (var r in records)
                await _projector.ProjectAsync(db, r, ct);

            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            // 4) mark committed
            agg.MarkChangesAsCommitted(expectedVersion + records.Count);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // якщо є UNIQUE(aggregateId, version) — це конкурентний апенд
            throw new ConcurrencyException($"Concurrent update detected. {ex}");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task EnsureStateAsync(
        AppDbContext db,
        Guid id,
        PositionUnitState from,
        PositionUnitState to,
        CancellationToken ct)
    {
        var rows = await db.PositionUnits
            .Where(x => x.Id == id && x.IsActived && x.State == from)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, to), ct);

        if (rows != 1)
            throw new InvalidOperationException($"Посада не в стані '{from}' або неактивна.");
    }

    private static PersonEventRecord ToRecord(IDomainEvent evt, Guid aggregateId, long version)
        => new()
        {
            AggregateId = aggregateId,
            Version = version,
            EventId = evt.EventId,
            EventType = evt.GetType().Name,
            Author = evt.Author,
            OccurredAtUtc = evt.OccurredAtUtc,
            EffectiveDate = GetEffectiveDate(evt),
            PayloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions),
        };

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

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
}