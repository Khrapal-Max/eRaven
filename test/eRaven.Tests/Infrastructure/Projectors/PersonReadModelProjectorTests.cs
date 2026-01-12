//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjectorTests (updated for simplified lifecycle + text Position)
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
            Bzvp: " A123 ",
            Weapon: " AK ",
            Callsign: " Fox ",
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
                enrollmentKind: null,                // ✅ empty on create
                enrollmentRef: null,
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: "Ivanovich",
                fullName: "Ivanov Ivan Ivanovich",
                rank: "Сержант",
                position: "Стрілець",
                bzvp: "A123",
                weapon: "AK",
                callsign: "Fox",
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
            Bzvp: null,
            Weapon: null,
            Callsign: null,
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
            Bzvp: null,
            Weapon: null,
            Callsign: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 123/ORD ",
            Reason: "reason",
            EnrollDate: new DateOnly(2026, 01, 10),
            Position: "Оператор",
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
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 2, excluded.EffectiveDate)); // Reserved + ExcludedAt set
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 3, enrolled.EnrollDate));   // Enrolled + ExcludedAt cleared
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
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: new DateOnly(2026, 01, 10),
                excludedAt: null,                 // ✅ cleared on enroll
                version: 3);
        }
    }

    [Fact]
    public async Task Excluded_sets_lifecycle_reserved_and_excluded_date_and_does_not_clear_other_fields()
    {
        var id = Guid.NewGuid();

        var created = new PersonCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            Rank: "Солдат",
            Position: "Оператор",
            Bzvp: "B",
            Weapon: "W",
            Callsign: "C",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.Unit,
            Reference: null,
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Position: "Оператор",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "excluded",
            EffectiveDate: new DateOnly(2026, 01, 31),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 2, enrolled.EnrollDate));
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 3, excluded.EffectiveDate));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Reserved, rm.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 31), rm.ExcludedAt);
            Assert.Equal(3, rm.Version);

            // ✅ must keep other fields
            Assert.Equal("Солдат", rm.Rank);
            Assert.Equal("Оператор", rm.Position);
            Assert.Equal("B", rm.Bzvp);
            Assert.Equal("W", rm.Weapon);
            Assert.Equal("C", rm.Callsign);
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
            Bzvp: null,
            Weapon: null,
            Callsign: null,
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
            Position: "Оператор",
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

            Assert.Null(after.Rank);      // ✅ rank voided
            Assert.Equal(4, after.Version); // ✅ last record version (includes void)
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
            Bzvp: null,
            Weapon: null,
            Callsign: null,
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

            Assert.Null(rm.Rank);         // ✅ rank voided
            Assert.Equal(3, rm.Version);  // ✅ last record
        }
    }
}
