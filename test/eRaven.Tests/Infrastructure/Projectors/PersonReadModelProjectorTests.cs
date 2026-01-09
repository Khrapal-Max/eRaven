//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjectorTests
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
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public async Task InitializeAsync()
    {
        _db = new SqliteTestDb();

        var ctx = _db.Factory.CreateDbContext();
        _projector = new PersonReadModelProjector(ctx);

        await Task.CompletedTask;
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
        string? plannedPosition,
        string? rank,
        string? position,
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

        Assert.Equal(plannedPosition, rm.PlannedPosition);

        Assert.Equal(rank, rm.Rank);
        Assert.Equal(position, rm.Position);
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

        var evt = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "Ivanovich"),
            PlannedPosition: " Planned ",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        await _projector.ProjectAsync(ToRecord(evt, version: 1));

        await using var ctx = _db.Factory.CreateDbContext();
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
            plannedPosition: "Planned",
            rank: null,
            position: null,
            temporaryPosition: null,
            bzvp: null,
            weapon: null,
            callsign: null,
            enrolledAt: null,
            excludedAt: null,
            version: 1);
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
            Author: "tester",
            OccurredAtUtc: NowUtc);

        await _projector.ProjectAsync(ToRecord(created, 1));

        var updatedSameVersion = new PersonPersonalInfoUpdated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(lastName: "NEW"),
            PlannedPosition: "X",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        // same version => ignore
        await _projector.ProjectAsync(ToRecord(updatedSameVersion, 1));

        await using var ctx = _db.Factory.CreateDbContext();
        var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal("Ivanov", rm.LastName);
        Assert.Null(rm.PlannedPosition);
        Assert.Equal(1, rm.Version);
    }

    [Fact]
    public async Task Enrolled_sets_lifecycle_and_enrollment_fields()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(middle: "M"),
            PlannedPosition: "P",
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var enrolled = new PersonEnrolled(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Kind: EnrollmentKind.AttachedByOrder,
            Reference: " 123/ORD ",
            Reason: "reason",
            EnrollDate: new DateOnly(2026, 01, 10),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        await _projector.ProjectAsync(ToRecord(created, 1));
        await _projector.ProjectAsync(ToRecord(enrolled, 2, enrolled.EnrollDate));

        await using var ctx = _db.Factory.CreateDbContext();
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
            plannedPosition: "P",
            rank: null,
            position: null,
            temporaryPosition: null,
            bzvp: null,
            weapon: null,
            callsign: null,
            enrolledAt: new DateOnly(2026, 01, 10),
            excludedAt: null,
            version: 2);
    }

    [Fact]
    public async Task Excluded_sets_lifecycle_and_excluded_date()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: null,
            Author: "tester",
            OccurredAtUtc: NowUtc);

        var excluded = new PersonExcluded(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Reason: "excluded",
            EffectiveDate: new DateOnly(2026, 01, 31),
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(1));

        await _projector.ProjectAsync(ToRecord(created, 1));
        await _projector.ProjectAsync(ToRecord(excluded, 2, excluded.EffectiveDate));

        await using var ctx = _db.Factory.CreateDbContext();
        var rm = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

        Assert.Equal(PersonLifecycle.Excluded, rm.Lifecycle);
        Assert.Equal(new DateOnly(2026, 01, 31), rm.ExcludedAt);
        Assert.Equal(2, rm.Version);
    }

    [Fact]
    public async Task Voided_event_triggers_rebuild_and_removes_effect_but_keeps_other_fields()
    {
        var id = Guid.NewGuid();

        var created = new PersonCandidateCreated(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            Personal: Personal(),
            PlannedPosition: "P",
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
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(3));

        var voided = new PersonEventVoided(
            EventId: Guid.NewGuid(),
            AggregateId: id,
            TargetEventId: rankEventId,
            Reason: "wrong rank",
            Author: "tester",
            OccurredAtUtc: NowUtc.AddMinutes(4));

        // зберігаємо історію в PersonEvents, щоб rebuild мав що читати
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

        // програємо інкрементально (як в проді)
        await _projector.ProjectAsync(ToRecord(created, 1));
        await _projector.ProjectAsync(ToRecord(rankChanged, 2));
        await _projector.ProjectAsync(ToRecord(positionChanged, 3));
        await _projector.ProjectAsync(ToRecord(enrolled, 4));

        // sanity before void
        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var before = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("Солдат", before.Rank);
            Assert.Equal("Оператор", before.Position);
            Assert.Equal(PersonLifecycle.Enrolled, before.Lifecycle);
            Assert.Equal(EnrollmentKind.AttachedByList, before.EnrollmentKind);
            Assert.Equal("LIST-9", before.EnrollmentReference);
            Assert.Equal(4, before.Version);
        }

        // void => rebuild
        await _projector.ProjectAsync(ToRecord(voided, 5));

        await using (var ctx = _db.Factory.CreateDbContext())
        {
            var after = await ctx.PersonRead.AsNoTracking().SingleAsync(x => x.Id == id);

            AssertAllFields(
                after,
                id: id,
                lifecycle: PersonLifecycle.Enrolled,                // enrollment лишився
                enrollmentKind: EnrollmentKind.AttachedByList,
                enrollmentRef: "LIST-9",
                rnokpp: "1234567890",
                lastName: "Ivanov",
                firstName: "Ivan",
                middleName: null,
                fullName: "Ivanov Ivan",
                plannedPosition: "P",
                rank: null,                                         // ✅ void rank
                position: "Оператор",                                // ✅ position лишився
                temporaryPosition: null,
                bzvp: null,
                weapon: null,
                callsign: null,
                enrolledAt: new DateOnly(2026, 01, 10),
                excludedAt: null,
                version: 5);
        }
    }
}
