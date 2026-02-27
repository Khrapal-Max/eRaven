//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonTaskEngagementAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Domain.Aggregates;

public sealed class PersonTaskEngagementAggregateTests
{
    private const string Author = "unit-test";
    private static readonly DateTime NowUtc = new(2026, 02, 27, 10, 00, 00, DateTimeKind.Utc);

    private static MissionAssignment AddCombatTask(
        Guid personId,
        Guid missionId,
        DateOnly from,
        DateOnly? toExclusive,
        Guid startDoc,
        Guid startDetails,
        Guid? endDoc = null,
        Guid? endDetails = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = toExclusive,
            SourceStartDocumentId = startDoc,
            SourceStartDetailsId = startDetails,
            SourceEndDocumentId = endDoc,
            SourceEndDetailsId = endDetails,
            UpdatedBy = Author,
            UpdatedAtUtc = NowUtc
        };

    [Fact]
    public void Ctor_Throws_WhenMultipleOpenIntervalsExist()
    {
        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var existing = new[]
        {
            AddCombatTask(personId, m1, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid()),
            AddCombatTask(personId, m2, new DateOnly(2026, 02, 10), null, Guid.NewGuid(), Guid.NewGuid()),
        };

        Assert.Throws<InvalidOperationException>(() => new PersonTaskEngagementAggregate(personId, existing));
    }

    [Fact]
    public void Ctor_Throws_WhenOpenIntervalIsNotLast()
    {
        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        // Open interval starts earlier, but there is a later closed interval -> invalid state.
        var existing = new[]
        {
            AddCombatTask(personId, m1, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid()),
            AddCombatTask(personId, m2, new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 12), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
        };

        Assert.Throws<InvalidOperationException>(() => new PersonTaskEngagementAggregate(personId, existing));
    }

    [Fact]
    public void ApplyStart_CreatesOpenInterval_AndReturnsStarted()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();

        var agg = new PersonTaskEngagementAggregate(personId, existing: null);

        var changes = agg.ApplyStart(
            missionId: missionId,
            from: new DateOnly(2026, 02, 01),
            startDocumentId: docId,
            startDetailsId: detailsId,
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc);

        Assert.Single(agg.Assignments);
        var a = agg.Assignments.Single();

        Assert.Equal(personId, a.PersonId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(new DateOnly(2026, 02, 01), a.From);
        Assert.Null(a.To);

        Assert.Equal(docId, a.SourceStartDocumentId);
        Assert.Equal(detailsId, a.SourceStartDetailsId);
        Assert.Null(a.SourceEndDocumentId);
        Assert.Null(a.SourceEndDetailsId);

        var started = Assert.Single(changes);
        Assert.Equal(EngagementChangeKind.Started, started.Kind);
        Assert.Equal(personId, started.PersonId);
        Assert.Equal(missionId, started.MissionId);
        Assert.Equal(new DateOnly(2026, 02, 01), started.EffectiveAt);
        Assert.Equal(docId, started.SourceDocumentId);
        Assert.Equal(detailsId, started.SourceDetailsId);
    }

    [Fact]
    public void ApplyStart_WithEndInclusive_NormalizesEndIds_AndReturnsStartedAndEnded()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();

        var agg = new PersonTaskEngagementAggregate(personId, existing: null);

        var changes = agg.ApplyStart(
            missionId: missionId,
            from: new DateOnly(2026, 02, 01),
            startDocumentId: docId,
            startDetailsId: detailsId,
            endInclusive: new DateOnly(2026, 02, 03), // inclusive
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc);

        Assert.Single(agg.Assignments);
        var a = agg.Assignments.Single();

        // To is exclusive
        Assert.Equal(new DateOnly(2026, 02, 04), a.To);

        // End source ids are normalized to Start ids when not provided.
        Assert.Equal(docId, a.SourceEndDocumentId);
        Assert.Equal(detailsId, a.SourceEndDetailsId);

        Assert.Equal(2, changes.Count);
        Assert.Equal(EngagementChangeKind.Started, changes[0].Kind);
        Assert.Equal(EngagementChangeKind.Ended, changes[1].Kind);

        Assert.Equal(new DateOnly(2026, 02, 04), changes[1].EffectiveAt);
        Assert.Equal(docId, changes[1].SourceDocumentId);
        Assert.Equal(detailsId, changes[1].SourceDetailsId);
    }

    [Fact]
    public void ApplyStart_IsIdempotent_ByStartDetailsId()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();

        var agg = new PersonTaskEngagementAggregate(personId, existing: null);

        var first = agg.ApplyStart(
            missionId,
            new DateOnly(2026, 02, 01),
            docId,
            detailsId,
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc);

        var second = agg.ApplyStart(
            missionId,
            new DateOnly(2026, 02, 01),
            startDocumentId: Guid.NewGuid(), // even with a different doc, idempotency is by DetailsId
            startDetailsId: detailsId,
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc);

        Assert.NotEmpty(first);
        Assert.Empty(second);
        Assert.Single(agg.Assignments);
    }

    [Fact]
    public void ApplyStart_Throws_WhenOpenIntervalExists()
    {
        var personId = Guid.NewGuid();
        var missionOpen = Guid.NewGuid();

        var open = AddCombatTask(personId, missionOpen, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid());
        var agg = new PersonTaskEngagementAggregate(personId, [open]);

        Assert.Throws<InvalidOperationException>(() => agg.ApplyStart(
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 10),
            startDocumentId: Guid.NewGuid(),
            startDetailsId: Guid.NewGuid(),
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc));
    }

    [Fact]
    public void ApplyStart_Throws_WhenOverlapsAndNotHandoverDay()
    {
        var personId = Guid.NewGuid();
        var mission1 = Guid.NewGuid();

        // Existing closed interval: [2026-02-01 .. 2026-02-11)
        var existing = AddCombatTask(
            personId,
            mission1,
            from: new DateOnly(2026, 02, 01),
            toExclusive: new DateOnly(2026, 02, 11),
            startDoc: Guid.NewGuid(),
            startDetails: Guid.NewGuid(),
            endDoc: Guid.NewGuid(),
            endDetails: Guid.NewGuid());

        var agg = new PersonTaskEngagementAggregate(personId, [existing]);

        // Start inside the interval (not last day) -> invalid overlap
        Assert.Throws<InvalidOperationException>(() => agg.ApplyStart(
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 05),
            startDocumentId: Guid.NewGuid(),
            startDetailsId: Guid.NewGuid(),
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc));
    }

    [Fact]
    public void ApplyStart_AllowsOverlap_OnHandoverDay()
    {
        var personId = Guid.NewGuid();
        var mission1 = Guid.NewGuid();

        // Existing closed interval: [2026-02-01 .. 2026-02-11) => last active day is 2026-02-10
        var existing = AddCombatTask(
            personId,
            mission1,
            from: new DateOnly(2026, 02, 01),
            toExclusive: new DateOnly(2026, 02, 11),
            startDoc: Guid.NewGuid(),
            startDetails: Guid.NewGuid(),
            endDoc: Guid.NewGuid(),
            endDetails: Guid.NewGuid());

        var agg = new PersonTaskEngagementAggregate(personId, [existing]);

        var changes = agg.ApplyStart(
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 10), // handover day == a.To - 1
            startDocumentId: Guid.NewGuid(),
            startDetailsId: Guid.NewGuid(),
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: Author,
            nowUtc: NowUtc);

        Assert.Equal(2, agg.Assignments.Count);
        Assert.Single(changes, c => c.Kind == EngagementChangeKind.Started);
    }

    [Fact]
    public void ApplyEnd_ClosesOpenInterval_AndReturnsEnded()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var startDoc = Guid.NewGuid();
        var startDetails = Guid.NewGuid();

        var open = AddCombatTask(personId, missionId, new DateOnly(2026, 02, 01), null, startDoc, startDetails);
        var agg = new PersonTaskEngagementAggregate(personId, [open]);

        var endDoc = Guid.NewGuid();
        var endDetails = Guid.NewGuid();

        var changes = agg.ApplyEnd(
            missionId: missionId,
            endInclusive: new DateOnly(2026, 02, 03),
            endDocumentId: endDoc,
            endDetailsId: endDetails,
            author: Author,
            nowUtc: NowUtc);

        var a = agg.Assignments.Single();
        Assert.Equal(new DateOnly(2026, 02, 04), a.To);
        Assert.Equal(endDoc, a.SourceEndDocumentId);
        Assert.Equal(endDetails, a.SourceEndDetailsId);

        var ended = Assert.Single(changes);
        Assert.Equal(EngagementChangeKind.Ended, ended.Kind);
        Assert.Equal(new DateOnly(2026, 02, 04), ended.EffectiveAt); // ToExclusive
    }

    [Fact]
    public void ApplyEnd_IsIdempotent_ByEndDetailsId()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var startDoc = Guid.NewGuid();
        var startDetails = Guid.NewGuid();

        var open = AddCombatTask(personId, missionId, new DateOnly(2026, 02, 01), null, startDoc, startDetails);
        var agg = new PersonTaskEngagementAggregate(personId, [open]);

        var endDoc = Guid.NewGuid();
        var endDetails = Guid.NewGuid();

        var first = agg.ApplyEnd(missionId, new DateOnly(2026, 02, 03), endDoc, endDetails, Author, NowUtc);
        var second = agg.ApplyEnd(missionId, new DateOnly(2026, 02, 03), endDoc, endDetails, Author, NowUtc);

        Assert.NotEmpty(first);
        Assert.Empty(second);
    }

    [Fact]
    public void ApplyEnd_Fallback_WhenNoOpenInterval_CreatesOneDayInterval()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var endDoc = Guid.NewGuid();
        var endDetails = Guid.NewGuid();

        var agg = new PersonTaskEngagementAggregate(personId, existing: null);

        var changes = agg.ApplyEnd(
            missionId: missionId,
            endInclusive: new DateOnly(2026, 02, 05),
            endDocumentId: endDoc,
            endDetailsId: endDetails,
            author: Author,
            nowUtc: NowUtc);

        Assert.Single(agg.Assignments);
        var a = agg.Assignments.Single();

        Assert.Equal(new DateOnly(2026, 02, 05), a.From);
        Assert.Equal(new DateOnly(2026, 02, 06), a.To);
        Assert.Equal(endDoc, a.SourceStartDocumentId);
        Assert.Equal(endDetails, a.SourceStartDetailsId);
        Assert.Equal(endDoc, a.SourceEndDocumentId);
        Assert.Equal(endDetails, a.SourceEndDetailsId);

        Assert.Equal(2, changes.Count);
        Assert.Equal(EngagementChangeKind.Started, changes[0].Kind);
        Assert.Equal(EngagementChangeKind.Ended, changes[1].Kind);
    }

    [Fact]
    public void CompensateDocument_RemovesStartedByDoc_AndReopensEndedByDoc()
    {
        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        // Valid initial state: closed interval first, open interval last.
        // 1) Closed interval ended by doc1
        var endedByDoc1 = AddCombatTask(
            personId,
            m2,
            from: new DateOnly(2026, 02, 01),
            toExclusive: new DateOnly(2026, 02, 03),
            startDoc: doc2,
            startDetails: Guid.NewGuid(),
            endDoc: doc1,
            endDetails: Guid.NewGuid());

        // 2) Open interval started by doc1 (will be removed)
        var startedByDoc1 = AddCombatTask(personId, m1, new DateOnly(2026, 02, 10), null, doc1, Guid.NewGuid());

        var agg = new PersonTaskEngagementAggregate(personId, [endedByDoc1, startedByDoc1]);

        var changes = agg.CompensateDocument(doc1, missionId: null, author: Author, nowUtc: NowUtc);

        // startedByDoc1 removed
        Assert.DoesNotContain(agg.Assignments, x => x.SourceStartDocumentId == doc1);

        // endedByDoc1 reopened
        var reopened = Assert.Single(agg.Assignments);
        Assert.Equal(m2, reopened.MissionId);
        Assert.Null(reopened.To);
        Assert.Null(reopened.SourceEndDocumentId);
        Assert.Null(reopened.SourceEndDetailsId);

        Assert.Contains(changes, c => c.Kind == EngagementChangeKind.Removed && c.MissionId == m1);
        Assert.Contains(changes, c => c.Kind == EngagementChangeKind.Reopened && c.MissionId == m2);
    }

    [Fact]
    public void CompensateDocument_WithMissionFilter_AffectsOnlyThatMission()
    {
        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var doc = Guid.NewGuid();

        // Valid state: closed interval first, open interval last.
        // m1: closed, started by doc
        var startedM1 = AddCombatTask(
            personId,
            m1,
            from: new DateOnly(2026, 02, 01),
            toExclusive: new DateOnly(2026, 02, 03),
            startDoc: doc,
            startDetails: Guid.NewGuid(),
            endDoc: doc,
            endDetails: Guid.NewGuid());

        // m2: open, started by doc
        var startedM2 = AddCombatTask(personId, m2, new DateOnly(2026, 02, 10), null, doc, Guid.NewGuid());

        var agg = new PersonTaskEngagementAggregate(personId, [startedM1, startedM2]);

        var changes = agg.CompensateDocument(doc, missionId: m2, author: Author, nowUtc: NowUtc);

        Assert.Contains(agg.Assignments, a => a.MissionId == m1); // untouched
        Assert.DoesNotContain(agg.Assignments, a => a.MissionId == m2); // removed (started by doc in m2)

        Assert.All(changes, c => Assert.Equal(m2, c.MissionId));
    }

    [Fact]
    public void CompensateDocument_Throws_WhenReopenWouldCreateSecondOpenInterval()
    {
        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var doc = Guid.NewGuid();
        var otherDoc = Guid.NewGuid();

        // Valid initial state:
        // 1) closed interval ended by "doc"
        var closedEndedByDoc = AddCombatTask(
            personId,
            m2,
            from: new DateOnly(2026, 02, 01),
            toExclusive: new DateOnly(2026, 02, 03),
            startDoc: otherDoc,
            startDetails: Guid.NewGuid(),
            endDoc: doc,
            endDetails: Guid.NewGuid());

        // 2) open interval (must be last)
        var open = AddCombatTask(personId, m1, new DateOnly(2026, 02, 10), null, otherDoc, Guid.NewGuid());

        var agg = new PersonTaskEngagementAggregate(personId, [closedEndedByDoc, open]);

        // Reopening the closed interval will create a second open interval -> invariant violation.
        Assert.Throws<InvalidOperationException>(() => agg.CompensateDocument(doc, missionId: null, author: Author, nowUtc: NowUtc));
    }
}
