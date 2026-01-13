//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjectorTests (updated for PositionSort)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure.Projectors;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Tests.Infrastructure.Projectors;

public sealed class PersonReadModelProjectorTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private PersonReadModelProjector _projector = default!;

    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _projector = new PersonReadModelProjector();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
        => await _db.DisposeAsync();

    // =========================
    // helpers
    // =========================

    private static PersonalInfo Personal(string lastName = "Ivanov", string firstName = "Ivan", string? middle = null)
        => new("1234567890", lastName, firstName, middle);

    private static PersonEventRecord ToRecord(IDomainEvent evt, long version, DateOnly? effectiveDate = null)
        => new()
        {
            EventId = evt.EventId,
            AggregateId = evt.AggregateId,
            Version = version,
            EventType = evt.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions),
            Author = evt.Author,
            OccurredAtUtc = evt.OccurredAtUtc,
            EffectiveDate = effectiveDate
        };

    private static void AssertAllFields(
        PersonReadModel rm,
        Guid id,
        PersonLifecycle lifecycle,
        EnrollmentKind? enrollmentKind,
        string? enrollmentRef,
        string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string fullName,
        string? rank,
        string? position,
        int? positionSort,
        string? bzvp,
        string? weapon,
        string? callsign,
        DateOnly? enrolledAt,
        DateOnly? excludedAt,
        long version)
    {
        Assert.Equal(id, rm.Id);
        Assert.Equal(lifecycle, rm.Lifecycle);

        Assert.Equal(enrollmentKind, rm.EnrollmentKind);
        Assert.Equal(enrollmentRef, rm.EnrollmentReference);

        Assert.Equal(rnokpp, rm.Rnokpp);
        Assert.Equal(lastName, rm.LastName);
        Assert.Equal(firstName, rm.FirstName);
        Assert.Equal(middleName, rm.MiddleName);
        Assert.Equal(fullName, rm.FullName);

        Assert.Equal(rank, rm.Rank);
        Assert.Equal(position, rm.Position);
        Assert.Equal(positionSort, rm.PositionSort);

        Assert.Equal(bzvp, rm.Bzvp);
        Assert.Equal(weapon, rm.Weapon);
        Assert.Equal(callsign, rm.Callsign);

        Assert.Equal(enrolledAt, rm.EnrolledAt);
        Assert.Equal(excludedAt, rm.ExcludedAt);

        Assert.Equal(version, rm.Version);
        Assert.NotEqual(default, rm.UpdatedAtUtc);
    }

    // =========================
    // tests
    // =========================

    [Fact]
    public async Task PersonCreated_creates_read_model_with_defaults_and_normalizes_fields()
    {
        var id = Guid.NewGuid();

        var evt = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "Ivanovich"),
            Rank: " Сержант ",
            Position: " Стрілець ",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(evt, version: 1));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            AssertAllFields(
                rm,
                id: id,
                lifecycle: PersonLifecycle.Reserved,
                enrollmentKind: null,
                enrollmentRef: null,
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: "Ivanovich",
                fullName: "Ivanov Ivan Ivanovich",
                rank: "Сержант",
                position: "Стрілець",
                positionSort: null,
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: null,
                excludedAt: null,
                version: 1);
        }
    }

    [Fact]
    public async Task Projector_is_idempotent_by_version()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var updatedSameVersion = new PersonPersonalInfoUpdated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(lastName: "NEW"),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(updatedSameVersion, 1)); // same version => ignore
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal("Ivanov", rm.LastName);
            Assert.Equal("Ivanov Ivan", rm.FullName);
            Assert.Equal(1, rm.Version);
        }
    }

    [Fact]
    public async Task Enrolled_sets_lifecycle_and_enrollment_fields_and_clears_excludedAt()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "M"),
            Rank: "Сержант",
            Position: "P",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 123/ORD ",
            Reason: "reason",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Сержант",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        // prepare: set excluded date, then enroll => must clear it
        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 05),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(0));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 2, excluded.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 3, enrolled.EnrollDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            AssertAllFields(
                rm,
                id: id,
                lifecycle: PersonLifecycle.Enrolled,
                enrollmentKind: EnrollmentKind.AttachedByOrder,
                enrollmentRef: "123/ORD",
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: "M",
                fullName: "Ivanov Ivan M",
                rank: "Сержант",
                position: "Оператор",
                positionSort: 9999,
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: new DateOnly(2026, 01, 10),
                excludedAt: null,
                version: 3);
        }
    }

    [Fact]
    public async Task Enrolled_after_excluded_sets_enrolled_and_clears_excludedAt()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "M"),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled1 = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 123/ORD ",
            Reason: "reason",
            EnrollDate: new DateOnly(2026, 01, 02),
            Rank: "Сержант",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 05),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var enrolled2 = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 999/ORD ",
            Reason: "re-enroll",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Сержант",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled1, 2, enrolled1.EnrollDate));
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 3, excluded.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled2, 4, enrolled2.EnrollDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Enrolled, rm.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 10), rm.EnrolledAt);
            Assert.Equal(EnrollmentKind.AttachedByOrder, rm.EnrollmentKind);
            Assert.Equal("999/ORD", rm.EnrollmentReference);

            Assert.Equal("Сержант", rm.Rank);
            Assert.Equal("Оператор", rm.Position);
            Assert.Equal(9999, rm.PositionSort);

            Assert.Null(rm.ExcludedAt);
            Assert.Equal(4, rm.Version);
        }
    }

    [Fact]
    public async Task Excluded_sets_lifecycle_reserved_and_excluded_date_and_does_not_clear_other_fields_including_bzvp_weapon_callsign()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: "Оператор",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.Unit,
            Reference: null,
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            Position: "Оператор",
            PositionSort: 10,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        // simulate "card changes" after enroll
        var bzvpChanged = new PersonBzvpChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 12),
            Bzvp: "B",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var weaponChanged = new PersonWeaponChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 12),
            Weapon: "W",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var callsignChanged = new PersonCallsignChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 12),
            Callsign: "C",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(4));

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "excluded",
            EffectiveDate: new DateOnly(2026, 01, 31),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(5));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 2, enrolled.EnrollDate));

            await _projector.ProjectAsync(ctx, ToRecord(bzvpChanged, 3, bzvpChanged.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(weaponChanged, 4, weaponChanged.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(callsignChanged, 5, callsignChanged.EffectiveDate));

            await _projector.ProjectAsync(ctx, ToRecord(excluded, 6, excluded.EffectiveDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Reserved, rm.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 31), rm.ExcludedAt);
            Assert.Equal(6, rm.Version);

            // ✅ must keep other fields
            Assert.Equal("Солдат", rm.Rank);
            Assert.Equal("Оператор", rm.Position);
            Assert.Equal(10, rm.PositionSort);

            // ✅ must keep "card fields" too (exclude shouldn't clear them)
            Assert.Equal("B", rm.Bzvp);
            Assert.Equal("W", rm.Weapon);
            Assert.Equal("C", rm.Callsign);

            Assert.Null(rm.EnrollmentKind);        // ✅ cleared on exclude
            Assert.Null(rm.EnrollmentReference);   // ✅ cleared on exclude
        }
    }

    [Fact]
    public async Task Voided_event_triggers_rebuild_and_removes_effect()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(0));

        var rankEventId = Guid.NewGuid();
        var rankChanged = new PersonRankChanged(
            EventId: rankEventId,
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 02),
            Rank: "Солдат",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByList,
            Reference: "LIST-9",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong rank",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        // IMPORTANT: rebuild reads db.PersonEvents, so we must persist full history including void
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonEvents.AddRange(
                ToRecord(created, 1),
                ToRecord(rankChanged, 2, rankChanged.EffectiveDate),
                ToRecord(enrolled, 3, enrolled.EnrollDate),
                ToRecord(voided, 4));

            await ctx.SaveChangesAsync();
        }

        // incremental projection (like prod): build read model before void
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(rankChanged, 2, rankChanged.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 3, enrolled.EnrollDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var before = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Солдат", before.Rank);
            Assert.Equal(PersonLifecycle.Enrolled, before.Lifecycle);
            Assert.Equal(3, before.Version);
        }

        // void => rebuild
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(voided, 4));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var after = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Enrolled, after.Lifecycle);
            Assert.Equal(EnrollmentKind.AttachedByList, after.EnrollmentKind);
            Assert.Equal("LIST-9", after.EnrollmentReference);
            Assert.Equal(new DateOnly(2026, 01, 10), after.EnrolledAt);

            Assert.Equal("Оператор", after.Position);
            Assert.Equal(9999, after.PositionSort);

            // ✅ because PersonEnrolled sets Rank
            Assert.Equal("Солдат", after.Rank);
            Assert.Equal(4, after.Version); // includes void
        }
    }

    [Fact]
    public async Task Voided_callsign_change_triggers_rebuild_and_removes_effect()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(0));

        var callsignEventId = Guid.NewGuid();
        var callsignChanged = new PersonCallsignChanged(
            EventId: callsignEventId,
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 02),
            Callsign: "Fox",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByList,
            Reference: "LIST-9",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: callsignEventId,
            Reason: "wrong callsign",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        // повна історія в event store (для rebuild)
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonEvents.AddRange(
                ToRecord(created, 1),
                ToRecord(callsignChanged, 2, callsignChanged.EffectiveDate),
                ToRecord(enrolled, 3, enrolled.EnrollDate),
                ToRecord(voided, 4));

            await ctx.SaveChangesAsync();
        }

        // інкрементальна проекція ДО void
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(callsignChanged, 2, callsignChanged.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 3, enrolled.EnrollDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var before = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Fox", before.Callsign);
            Assert.Equal(3, before.Version);
        }

        // void => rebuild
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(voided, 4));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var after = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Enrolled, after.Lifecycle);
            Assert.Equal("Солдат", after.Rank);
            Assert.Equal("Оператор", after.Position);
            Assert.Equal(9999, after.PositionSort);

            Assert.Null(after.Callsign); // ✅ removed by rebuild
            Assert.Equal(4, after.Version);
        }
    }

    [Fact]
    public async Task Voided_excluded_triggers_rebuild_and_restores_enrolled_state()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: null,
            Position: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByList,
            Reference: "LIST-9",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            Position: "Оператор",
            PositionSort: 9999,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var excludedEventId = Guid.NewGuid();
        var excluded = new PersonExcluded(
            EventId: excludedEventId,
            AggregateId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: excludedEventId,
            Reason: "restore",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        // rebuild uses db.PersonEvents
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonEvents.AddRange(
                ToRecord(created, 1),
                ToRecord(enrolled, 2, enrolled.EnrollDate),
                ToRecord(excluded, 3, excluded.EffectiveDate),
                ToRecord(voided, 4)
            );

            await ctx.SaveChangesAsync();
        }

        // incremental projection up to excluded
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 2, enrolled.EnrollDate));
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 3, excluded.EffectiveDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var before = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal(PersonLifecycle.Reserved, before.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 20), before.ExcludedAt);
            Assert.Equal(3, before.Version);
        }

        // void => rebuild
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(voided, 4));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var after = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Enrolled, after.Lifecycle);
            Assert.Null(after.ExcludedAt);
            Assert.Equal(new DateOnly(2026, 01, 10), after.EnrolledAt);

            Assert.Equal("Солдат", after.Rank);
            Assert.Equal("Оператор", after.Position);
            Assert.Equal(9999, after.PositionSort);

            Assert.Equal(EnrollmentKind.AttachedByList, after.EnrollmentKind);
            Assert.Equal("LIST-9", after.EnrollmentReference);

            Assert.Equal(4, after.Version);
        }
    }

    [Fact]
    public async Task RebuildAsync_when_no_events_should_remove_read_model()
    {
        var id = Guid.NewGuid();

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonRead.Add(new PersonReadModel
            {
                Id = id,
                Lifecycle = PersonLifecycle.Reserved,
                Rnokpp = "1234567890",
                LastName = "X",
                FirstName = "Y",
                FullName = "X Y",
                Version = 1,
                UpdatedAtUtc = NowUtc
            });

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.RebuildAsync(ctx, id, default);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.SingleOrDefaultAsync(x => x.Id == id);
            Assert.Null(rm);
        }
    }

    [Fact]
    public async Task RebuildAsync_should_rebuild_from_events_ignoring_voided()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: new PersonalInfo("1234567890", "Ivanov", "Ivan", null),
            Rank: null,
            Position: null,
            Author: "t",
            OccurredAtUtc: NowUtc);

        var rankEventId = Guid.NewGuid();
        var rankChanged = new PersonRankChanged(
            EventId: rankEventId,
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 02),
            Rank: "Солдат",
            Note: null,
            Author: "t",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong",
            Author: "t",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonEvents.AddRange(
                ToRecord(created, 1),
                ToRecord(rankChanged, 2, rankChanged.EffectiveDate),
                ToRecord(voided, 3));

            // "битий" read-model, щоб rebuild точно перезаписав
            ctx.PersonRead.Add(new PersonReadModel
            {
                Id = id,
                Lifecycle = PersonLifecycle.Enrolled,
                Rnokpp = "1234567890",
                LastName = "Old",
                FirstName = "Old",
                FullName = "Old Old",
                Rank = "WRONG",
                Position = "WRONG",
                PositionSort = 777,
                Version = 999,
                UpdatedAtUtc = NowUtc
            });

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.RebuildAsync(ctx, id, default);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Reserved, rm.Lifecycle); // from PersonCreated
            Assert.Equal("Ivanov", rm.LastName);
            Assert.Equal("Ivanov Ivan", rm.FullName);

            Assert.Null(rm.Rank);          // ✅ rank voided
            Assert.Null(rm.PositionSort);  // ✅ nothing set (created had null position)
            Assert.Equal(3, rm.Version);   // ✅ last record
        }
    }
}
