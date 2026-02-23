//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTaskEngagementAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Domain.Aggregates;

public sealed class PersonTaskEngagementAggregateTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 23, 8, 0, 0, DateTimeKind.Utc);

    private static MissionAssignment MakeAssignment(
        Guid personId,
        Guid missionId,
        DateOnly from,
        DateOnly? toExclusive,
        Guid startDocId,
        Guid startDetailsId,
        Guid? endDocId = null,
        Guid? endDetailsId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = toExclusive,
            SourceStartDocumentId = startDocId,
            SourceStartDetailsId = startDetailsId,
            SourceEndDocumentId = endDocId,
            SourceEndDetailsId = endDetailsId,
            UpdatedBy = "seed",
            UpdatedAtUtc = NowUtc
        };

    [Fact]
    public void Ctor_SortsExistingIntervals_ByFrom()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var d1 = new DateOnly(2026, 02, 10);
        var d2 = new DateOnly(2026, 02, 01);

        var a1 = MakeAssignment(personId, missionId, d1, d1.AddDays(1), Guid.NewGuid(), Guid.NewGuid());
        var a2 = MakeAssignment(personId, missionId, d2, d2.AddDays(1), Guid.NewGuid(), Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [a1, a2]);

        Assert.Equal(2, aggr.Assignments.Count);
        Assert.Equal(d2, aggr.Assignments[0].From);
        Assert.Equal(d1, aggr.Assignments[1].From);
    }

    [Fact]
    public void Ctor_WhenMultipleOpenIntervals_Throws()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var a1 = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid());
        var a2 = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 10), null, Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => new PersonTaskEngagementAggregate(personId, [a1, a2]));
    }

    [Fact]
    public void ApplyStart_AddsOpenInterval_AndReturnsStartedChange()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var startDocId = Guid.NewGuid();
        var startDetailsId = Guid.NewGuid();
        var from = new DateOnly(2026, 02, 01);

        var aggr = new PersonTaskEngagementAggregate(personId, existing: null);

        var changes = aggr.ApplyStart(
            missionId: missionId,
            from: from,
            startDocumentId: startDocId,
            startDetailsId: startDetailsId,
            endInclusive: null,
            endDocumentId: null,
            endDetailsId: null,
            author: "u",
            nowUtc: NowUtc);

        Assert.Single(aggr.Assignments);
        var a = aggr.Assignments.Single();

        Assert.Equal(personId, a.PersonId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(from, a.From);
        Assert.Null(a.To);
        Assert.Equal(startDocId, a.SourceStartDocumentId);
        Assert.Equal(startDetailsId, a.SourceStartDetailsId);
        Assert.Null(a.SourceEndDocumentId);
        Assert.Null(a.SourceEndDetailsId);

        Assert.Single(changes);
        Assert.Equal(EngagementChangeKind.Started, changes[0].Kind);
        Assert.Equal(from, changes[0].EffectiveAt);
        Assert.Equal(startDocId, changes[0].SourceDocumentId);
        Assert.Equal(startDetailsId, changes[0].SourceDetailsId);
    }

    [Fact]
    public void ApplyStart_WithEndInclusive_CreatesClosedInterval_AndReturnsStartedAndEnded()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var startDetailsId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();

        var from = new DateOnly(2026, 02, 01);
        var endInclusive = new DateOnly(2026, 02, 10);
        var toExclusive = new DateOnly(2026, 02, 11);

        var aggr = new PersonTaskEngagementAggregate(personId, existing: null);

        var changes = aggr.ApplyStart(
            missionId: missionId,
            from: from,
            startDocumentId: docId,
            startDetailsId: startDetailsId,
            endInclusive: endInclusive,
            endDocumentId: docId,
            endDetailsId: endDetailsId,
            author: "u",
            nowUtc: NowUtc);

        var a = aggr.Assignments.Single();
        Assert.Equal(from, a.From);
        Assert.Equal(toExclusive, a.To);
        Assert.Equal(docId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);

        Assert.Equal(2, changes.Count);
        Assert.Equal(EngagementChangeKind.Started, changes[0].Kind);
        Assert.Equal(from, changes[0].EffectiveAt);
        Assert.Equal(EngagementChangeKind.Ended, changes[1].Kind);
        Assert.Equal(toExclusive, changes[1].EffectiveAt); // ToExclusive
    }

    [Fact]
    public void ApplyStart_Idempotent_OnSameStartDetail()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();
        var from = new DateOnly(2026, 02, 01);

        var aggr = new PersonTaskEngagementAggregate(personId, existing: null);

        var c1 = aggr.ApplyStart(missionId, from, docId, detailsId, null, null, null, "u", NowUtc);
        var c2 = aggr.ApplyStart(missionId, from, docId, detailsId, null, null, null, "u", NowUtc);

        Assert.Single(aggr.Assignments);
        Assert.Single(c1);
        Assert.Empty(c2);
    }

    [Fact]
    public void ApplyStart_WhenOpenIntervalExists_Throws()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var existing = MakeAssignment(
            personId,
            missionId,
            from: new DateOnly(2026, 02, 01),
            toExclusive: null,
            startDocId: Guid.NewGuid(),
            startDetailsId: Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [existing]);

        Assert.Throws<InvalidOperationException>(() =>
            aggr.ApplyStart(missionId, new DateOnly(2026, 02, 10), Guid.NewGuid(), Guid.NewGuid(), null, null, null, "u", NowUtc));
    }

    [Fact]
    public void ApplyStart_AllowsHandoverDayOverlap_WhenStartsOnEndInclusive()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        // existing: [2026-02-01 .. 2026-02-11)  => EndInclusive = 2026-02-10
        var existing = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 01),
            new DateOnly(2026, 02, 11),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [existing]);

        // new starts on EndInclusive day (2026-02-10) => allowed handover overlap
        var ex = Record.Exception(() =>
            aggr.ApplyStart(
                missionId,
                new DateOnly(2026, 02, 10),
                Guid.NewGuid(),
                Guid.NewGuid(),
                endInclusive: new DateOnly(2026, 02, 15),
                endDocumentId: Guid.NewGuid(),
                endDetailsId: Guid.NewGuid(),
                author: "u",
                nowUtc: NowUtc));

        Assert.Null(ex);
        Assert.Equal(2, aggr.Assignments.Count);
        Assert.Contains(aggr.Assignments, a => a.From == new DateOnly(2026, 02, 10));
    }

    [Fact]
    public void ApplyStart_WhenOverlapsMoreThanHandoverDay_Throws()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        // existing: [2026-02-01 .. 2026-02-11) => EndInclusive = 2026-02-10
        var existing = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 01),
            new DateOnly(2026, 02, 11),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [existing]);

        // new starts earlier than EndInclusive => overlaps more than 1 day => must throw
        Assert.Throws<InvalidOperationException>(() =>
            aggr.ApplyStart(
                missionId,
                new DateOnly(2026, 02, 09),
                Guid.NewGuid(),
                Guid.NewGuid(),
                endInclusive: new DateOnly(2026, 02, 15),
                endDocumentId: Guid.NewGuid(),
                endDetailsId: Guid.NewGuid(),
                author: "u",
                nowUtc: NowUtc));
    }

    [Fact]
    public void ApplyStart_AllowsBackToBackIntervals_NoOverlap()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        // existing: [2026-02-01 .. 2026-02-11)
        var existing = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 01),
            new DateOnly(2026, 02, 11),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [existing]);

        // new starts exactly at previous ToExclusive => ok: [2026-02-11 .. 2026-02-13)
        var changes = aggr.ApplyStart(
            missionId,
            new DateOnly(2026, 02, 11),
            Guid.NewGuid(),
            Guid.NewGuid(),
            endInclusive: new DateOnly(2026, 02, 12),
            endDocumentId: Guid.NewGuid(),
            endDetailsId: Guid.NewGuid(),
            author: "u",
            nowUtc: NowUtc);

        Assert.Equal(2, aggr.Assignments.Count);
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void ApplyEnd_ClosesOpenIntervalForMission_AndReturnsEndedChange()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var startDocId = Guid.NewGuid();
        var startDetailsId = Guid.NewGuid();

        var open = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 01),
            null,
            startDocId,
            startDetailsId);

        var aggr = new PersonTaskEngagementAggregate(personId, [open]);

        var endDocId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();
        var endInclusive = new DateOnly(2026, 02, 10);
        var toExclusive = new DateOnly(2026, 02, 11);

        var changes = aggr.ApplyEnd(missionId, endInclusive, endDocId, endDetailsId, "u", NowUtc);

        var a = aggr.Assignments.Single();
        Assert.Equal(toExclusive, a.To);
        Assert.Equal(endDocId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);

        Assert.Single(changes);
        Assert.Equal(EngagementChangeKind.Ended, changes[0].Kind);
        Assert.Equal(toExclusive, changes[0].EffectiveAt);
    }

    [Fact]
    public void ApplyEnd_Idempotent_OnSameEndDetail()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var open = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid());
        var aggr = new PersonTaskEngagementAggregate(personId, [open]);

        var endDocId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();

        var c1 = aggr.ApplyEnd(missionId, new DateOnly(2026, 02, 10), endDocId, endDetailsId, "u", NowUtc);
        var c2 = aggr.ApplyEnd(missionId, new DateOnly(2026, 02, 10), endDocId, endDetailsId, "u", NowUtc);

        Assert.Single(c1);
        Assert.Empty(c2);
    }

    [Fact]
    public void ApplyEnd_WhenEndBeforeStart_Throws()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var open = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 10), null, Guid.NewGuid(), Guid.NewGuid());
        var aggr = new PersonTaskEngagementAggregate(personId, [open]);

        Assert.Throws<InvalidOperationException>(() =>
            aggr.ApplyEnd(missionId, new DateOnly(2026, 02, 09), Guid.NewGuid(), Guid.NewGuid(), "u", NowUtc));
    }

    [Fact]
    public void ApplyEnd_WhenNoOpenInterval_CreatesOneDayInterval_AndReturnsStartedAndEnded()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var aggr = new PersonTaskEngagementAggregate(personId, existing: null);

        var endDocId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();
        var endInclusive = new DateOnly(2026, 02, 10);
        var toExclusive = new DateOnly(2026, 02, 11);

        var changes = aggr.ApplyEnd(missionId, endInclusive, endDocId, endDetailsId, "u", NowUtc);

        Assert.Single(aggr.Assignments);
        var a = aggr.Assignments.Single();

        Assert.Equal(endInclusive, a.From);
        Assert.Equal(toExclusive, a.To);
        Assert.Equal(endDocId, a.SourceStartDocumentId);
        Assert.Equal(endDetailsId, a.SourceStartDetailsId);
        Assert.Equal(endDocId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);

        Assert.Equal(2, changes.Count);
        Assert.Equal(EngagementChangeKind.Started, changes[0].Kind);
        Assert.Equal(endInclusive, changes[0].EffectiveAt);
        Assert.Equal(EngagementChangeKind.Ended, changes[1].Kind);
        Assert.Equal(toExclusive, changes[1].EffectiveAt);
    }

    [Fact]
    public void ApplyEnd_WhenOpenIntervalExistsForOtherMission_FallbackThrowsByInvariant()
    {
        var personId = Guid.NewGuid();
        var missionA = Guid.NewGuid();
        var missionB = Guid.NewGuid();

        var openA = MakeAssignment(personId, missionA, new DateOnly(2026, 02, 01), null, Guid.NewGuid(), Guid.NewGuid());
        var aggr = new PersonTaskEngagementAggregate(personId, [openA]);

        // closing missionB while missionA is open -> open interval exists, so EnsureCanStart will throw.
        Assert.Throws<InvalidOperationException>(() =>
            aggr.ApplyEnd(missionB, new DateOnly(2026, 02, 10), Guid.NewGuid(), Guid.NewGuid(), "u", NowUtc));
    }

    [Fact]
    public void CompensateDocument_RemovesStartedIntervals_AndReopensEndedIntervals()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var startDoc = Guid.NewGuid();
        var endDoc = Guid.NewGuid();

        var aStartedByDoc = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 01),
            new DateOnly(2026, 02, 05),
            startDoc,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var aEndedByDoc = MakeAssignment(
            personId,
            missionId,
            new DateOnly(2026, 02, 10),
            new DateOnly(2026, 02, 16),
            Guid.NewGuid(),
            Guid.NewGuid(),
            endDoc,
            Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [aStartedByDoc, aEndedByDoc]);

        var changes = aggr.CompensateDocument(startDoc, missionId: null, author: "u", nowUtc: NowUtc);

        Assert.Single(aggr.Assignments);
        Assert.DoesNotContain(aggr.Assignments, a => a.SourceStartDocumentId == startDoc);

        // Now compensate the end document: should reopen.
        var changes2 = aggr.CompensateDocument(endDoc, missionId: null, author: "u", nowUtc: NowUtc);
        var reopened = aggr.Assignments.Single();
        Assert.Null(reopened.To);
        Assert.Null(reopened.SourceEndDocumentId);
        Assert.Null(reopened.SourceEndDetailsId);

        Assert.Contains(changes, c => c.Kind == EngagementChangeKind.Removed);
        Assert.Contains(changes2, c => c.Kind == EngagementChangeKind.Reopened);
    }

    [Fact]
    public void CompensateDocument_FilterByMissionId_AffectsOnlySpecifiedMission()
    {
        var personId = Guid.NewGuid();
        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();
        var docId = Guid.NewGuid();

        var a1 = MakeAssignment(personId, mission1, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 05), docId, Guid.NewGuid());
        var a2 = MakeAssignment(personId, mission2, new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 16), docId, Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [a1, a2]);

        var changes = aggr.CompensateDocument(docId, missionId: mission1, author: "u", nowUtc: NowUtc);

        Assert.Single(aggr.Assignments);
        Assert.Equal(mission2, aggr.Assignments.Single().MissionId);
        Assert.Single(changes);
        Assert.Equal(EngagementChangeKind.Removed, changes[0].Kind);
        Assert.Equal(mission1, changes[0].MissionId);
    }

    [Fact]
    public void CompensateDocument_WhenWouldCreateMultipleOpenIntervals_Throws()
    {
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var closeDoc = Guid.NewGuid();

        // Two closed intervals both ended by the same document.
        var a1 = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 05), Guid.NewGuid(), Guid.NewGuid(), closeDoc, Guid.NewGuid());
        var a2 = MakeAssignment(personId, missionId, new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 16), Guid.NewGuid(), Guid.NewGuid(), closeDoc, Guid.NewGuid());

        var aggr = new PersonTaskEngagementAggregate(personId, [a1, a2]);

        // Reopening both would create two open-ended intervals => should throw.
        Assert.Throws<InvalidOperationException>(() =>
            aggr.CompensateDocument(closeDoc, missionId: null, author: "u", nowUtc: NowUtc));
    }
}
