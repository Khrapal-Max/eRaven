//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjectorTests (updated for PlannedPositionUnitId / PositionUnitId)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events;
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
        EnrollmentKind enrollmentKind,
        string? enrollmentRef,
        string rnokpp,
        string lastName,
        string firstName,
        string? middleName,
        string fullName,
        Guid? plannedPositionUnitId,
        string? plannedPosition,
        Guid? positionUnitId,
        string? position,
        Guid? temporaryPositionUnitId,
        string? temporaryPosition,
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

        Assert.Equal(plannedPositionUnitId, rm.PlannedPositionUnitId);
        Assert.Equal(plannedPosition, rm.PlannedPosition);

        Assert.Equal(positionUnitId, rm.PositionUnitId);
        Assert.Equal(position, rm.Position);

        Assert.Equal(temporaryPositionUnitId, rm.TemporaryPositionUnitId);
        Assert.Equal(temporaryPosition, rm.TemporaryPosition);

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
    public async Task CandidateCreated_creates_read_model_with_defaults()
    {
        var id = Guid.NewGuid();
        var plannedPosId = Guid.NewGuid();

        var evt = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "Ivanovich"),
            PlannedPosition: " Planned ",
            PlannedPositionUnitId: plannedPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(evt, version: 1));
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            AssertAllFields(
                rm,
                id: id,
                lifecycle: PersonLifecycle.Candidate,
                enrollmentKind: EnrollmentKind.Unit,
                enrollmentRef: null,
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: "Ivanovich",
                fullName: "Ivanov Ivan Ivanovich",
                plannedPositionUnitId: plannedPosId,
                plannedPosition: "Planned",
                positionUnitId: null,
                position: null,
                temporaryPositionUnitId: null,
                temporaryPosition: null,
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

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            PlannedPositionUnitId: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var updatedSameVersion = new PersonPersonalInfoUpdated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(lastName: "NEW"),
            PlannedPosition: "X",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(updatedSameVersion, 1)); // same version => ignore
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal("Ivanov", rm.LastName);
            Assert.Null(rm.PlannedPosition);
            Assert.Equal(1, rm.Version);
        }
    }

    [Fact]
    public async Task Enrolled_sets_lifecycle_and_enrollment_fields_and_clears_planned_position_unit_id()
    {
        var id = Guid.NewGuid();
        var plannedPosId = Guid.NewGuid();
        var mainPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "M"),
            PlannedPosition: "P",
            PlannedPositionUnitId: plannedPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 123/ORD ",
            Reason: "reason",
            EnrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: mainPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 2, enrolled.EnrollDate));
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
                plannedPositionUnitId: null,          // ✅ cleared
                plannedPosition: "P",
                positionUnitId: mainPosId,            // ✅ set
                position: null,
                temporaryPositionUnitId: null,
                temporaryPosition: null,
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: new DateOnly(2026, 01, 10),
                excludedAt: null,
                version: 2);
        }
    }

    [Fact]
    public async Task Excluded_sets_lifecycle_and_excluded_date_and_clears_position_ids()
    {
        var id = Guid.NewGuid();
        var plannedPosId = Guid.NewGuid();
        var mainPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
            PlannedPositionUnitId: plannedPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var posChanged = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 03),
            PositionUnitId: mainPosId,
            Position: "Оператор",
            Note: null,
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
            await _projector.ProjectAsync(ctx, ToRecord(posChanged, 2, posChanged.EffectiveDate));
            await _projector.ProjectAsync(ctx, ToRecord(excluded, 3, excluded.EffectiveDate));
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(PersonLifecycle.Excluded, rm.Lifecycle);
            Assert.Equal(new DateOnly(2026, 01, 31), rm.ExcludedAt);
            Assert.Equal(3, rm.Version);

            Assert.Null(rm.PlannedPositionUnitId);
            Assert.Null(rm.PositionUnitId);
            Assert.Null(rm.TemporaryPositionUnitId);
        }
    }

    [Fact]
    public async Task Voided_event_triggers_rebuild_and_removes_effect_but_keeps_other_fields_and_ids()
    {
        var id = Guid.NewGuid();

        var plannedPosId = Guid.NewGuid();
        var mainPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
            PlannedPositionUnitId: plannedPosId,
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

        var positionChanged = new PersonPositionChanged(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            EffectiveDate: new DateOnly(2026, 01, 03),
            PositionUnitId: mainPosId,
            Position: "Оператор",
            Note: null,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(2));

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByList,
            Reference: "LIST-9",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            PositionUnitId: mainPosId,
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong rank",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(4));

        // История в PersonEvents, чтобы rebuild мог читать
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            ctx.PersonEvents.AddRange(
                ToRecord(created, 1),
                ToRecord(rankChanged, 2, rankChanged.EffectiveDate),
                ToRecord(positionChanged, 3, positionChanged.EffectiveDate),
                ToRecord(enrolled, 4, enrolled.EnrollDate),
                ToRecord(voided, 5));

            await ctx.SaveChangesAsync();
        }

        // Инкрементально (как в проде)
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(created, 1));
            await _projector.ProjectAsync(ctx, ToRecord(rankChanged, 2));
            await _projector.ProjectAsync(ctx, ToRecord(positionChanged, 3));
            await _projector.ProjectAsync(ctx, ToRecord(enrolled, 4));
        }

        // sanity before void
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var before = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Солдат", before.Rank);
            Assert.Equal("Оператор", before.Position);
            Assert.Equal(mainPosId, before.PositionUnitId);
            Assert.Equal(PersonLifecycle.Enrolled, before.Lifecycle);

            Assert.Null(before.PlannedPositionUnitId); // уже очищен на Enrolled
            Assert.Equal(4, before.Version);
        }

        // void => rebuild
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.ProjectAsync(ctx, ToRecord(voided, 5));
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var after = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            AssertAllFields(
                after,
                id: id,
                lifecycle: PersonLifecycle.Enrolled,
                enrollmentKind: EnrollmentKind.AttachedByList,
                enrollmentRef: "LIST-9",
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: null,
                fullName: "Ivanov Ivan",
                plannedPositionUnitId: null,
                plannedPosition: "P",
                positionUnitId: mainPosId,
                position: "Оператор",
                temporaryPositionUnitId: null,
                temporaryPosition: null,
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: new DateOnly(2026, 01, 10),
                excludedAt: null,
                version: 5);

            Assert.Null(after.Rank); // ✅ rank voided
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
                Rnokpp = "1234567890",
                FullName = "X",
                Version = 1,
                UpdatedAtUtc = NowUtc
            });

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.RebuildAsync(ctx, id, default);
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
        var plannedPosId = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: new PersonalInfo("1234567890", "Ivanov", "Ivan", null),
            PlannedPosition: "P",
            PlannedPositionUnitId: plannedPosId,
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

            // "битый" read-model, чтобы rebuild точно перезаписал
            ctx.PersonRead.Add(new PersonReadModel
            {
                Id = id,
                Rnokpp = "1234567890",
                FullName = "Old",
                Rank = "WRONG",
                PlannedPosition = "OLD",
                PlannedPositionUnitId = null,
                Version = 999,
                UpdatedAtUtc = NowUtc
            });

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            await _projector.RebuildAsync(ctx, id, default);
        }

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            Assert.Equal(plannedPosId, rm.PlannedPositionUnitId); // ✅ вместо NotNull
            Assert.Equal("P", rm.PlannedPosition);
            Assert.Null(rm.Rank);
            Assert.Equal(3, rm.Version);
        }
    }
}
