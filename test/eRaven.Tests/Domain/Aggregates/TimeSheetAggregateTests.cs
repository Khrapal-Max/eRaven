//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Domain.Aggregates;

public sealed class TimeSheetAggregateTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc);

    private static TimeSheetAggregate NewEpisode(
        DateOnly openedAt,
        DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };

    // =========================
    // basics
    // =========================

    [Fact]
    public void IsActiveOn_when_open_ended_should_be_true_from_openedAt_and_false_before()
    {
        var openedAt = new DateOnly(2026, 02, 10);
        var sut = NewEpisode(openedAt);

        Assert.False(sut.IsActiveOn(openedAt.AddDays(-1)));
        Assert.True(sut.IsActiveOn(openedAt));
        Assert.True(sut.IsActiveOn(openedAt.AddDays(10)));
    }

    [Fact]
    public void IsActiveOn_when_closed_should_be_inclusive_of_closedAt()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 12));

        Assert.True(sut.IsActiveOn(new DateOnly(2026, 02, 10)));
        Assert.True(sut.IsActiveOn(new DateOnly(2026, 02, 12)));
        Assert.False(sut.IsActiveOn(new DateOnly(2026, 02, 13)));
    }

    [Fact]
    public void HasActiveTaskOn_should_respect_span_is_active_on_and_ignore_canceled()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.TaskSpans.Add(new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = sut.Id,
            PersonId = sut.PersonId,
            CombatTaskDocumentId = Guid.NewGuid(),
            MissionId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 02, 10),
            ToDate = new DateOnly(2026, 02, 10),
            Status = DocumentStatus.Canceled,
            UpdatedBy = "tester",
            UpdatedAtUtc = NowUtc
        });

        sut.TaskSpans.Add(new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = sut.Id,
            PersonId = sut.PersonId,
            CombatTaskDocumentId = Guid.NewGuid(),
            MissionId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 02, 11),
            ToDate = null,
            Status = DocumentStatus.Draft,
            UpdatedBy = "tester",
            UpdatedAtUtc = NowUtc
        });

        Assert.False(sut.HasActiveTaskOn(new DateOnly(2026, 02, 10)));
        Assert.True(sut.HasActiveTaskOn(new DateOnly(2026, 02, 11)));
        Assert.True(sut.HasActiveTaskOn(new DateOnly(2026, 03, 01)));
    }

    [Fact]
    public void EnsureNotClosed_when_closed_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 05));

        var ex = Assert.Throws<InvalidOperationException>(() => sut.EnsureNotClosed());
        Assert.Equal("Timesheet episode is closed.", ex.Message);
    }

    [Fact]
    public void EnsureInBounds_when_before_openedAt_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 10));

        var ex = Assert.Throws<InvalidOperationException>(() => sut.EnsureInBounds(new DateOnly(2026, 02, 09)));
        Assert.Contains("before OpenedAt", ex.Message);
    }

    [Fact]
    public void EnsureInBounds_when_after_closedAt_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 10), new DateOnly(2026, 02, 12));

        var ex = Assert.Throws<InvalidOperationException>(() => sut.EnsureInBounds(new DateOnly(2026, 02, 13)));
        Assert.Contains("after ClosedAt", ex.Message);
    }

    // =========================
    // UpsertTask
    // =========================

    [Fact]
    public void UpsertTask_when_span_missing_should_create_span_and_set_denormalized_fields()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        sut.UpsertTask(
            documentId: docId,
            missionId: missionId,
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 12),
            status: DocumentStatus.Draft,
            author: "planner",
            nowUtc: NowUtc);

        Assert.Single(sut.TaskSpans);

        var span = sut.TaskSpans[0];
        Assert.NotEqual(Guid.Empty, span.Id);
        Assert.Equal(sut.Id, span.TimesheetId);
        Assert.Equal(sut.PersonId, span.PersonId);
        Assert.Equal(docId, span.CombatTaskDocumentId);
        Assert.Equal(missionId, span.MissionId);
        Assert.Equal(new DateOnly(2026, 02, 10), span.FromDate);
        Assert.Equal(new DateOnly(2026, 02, 12), span.ToDate);
        Assert.Equal(DocumentStatus.Draft, span.Status);
        Assert.Equal("planner", span.UpdatedBy);
        Assert.Equal(NowUtc, span.UpdatedAtUtc);
        Assert.Null(span.ClosedByCodeId);
        Assert.Null(span.ClosedReference);
    }

    [Fact]
    public void UpsertTask_when_status_not_canceled_should_clear_closed_fields()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        sut.TaskSpans.Add(new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = sut.Id,
            PersonId = sut.PersonId,
            CombatTaskDocumentId = docId,
            MissionId = missionId,
            FromDate = new DateOnly(2026, 02, 10),
            ToDate = new DateOnly(2026, 02, 12),
            Status = DocumentStatus.Canceled,
            ClosedByCodeId = Guid.NewGuid(),
            ClosedReference = "ref",
            UpdatedBy = "tester",
            UpdatedAtUtc = NowUtc.AddMinutes(-1)
        });

        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 12),
            status: DocumentStatus.Draft,
            author: "planner",
            nowUtc: NowUtc);

        Assert.Single(sut.TaskSpans);
        var span = sut.TaskSpans[0];
        Assert.Equal(DocumentStatus.Draft, span.Status);
        Assert.Null(span.ClosedByCodeId);
        Assert.Null(span.ClosedReference);
    }

    [Fact]
    public void UpsertTask_when_to_before_from_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.UpsertTask(Guid.NewGuid(), Guid.NewGuid(),
                from: new DateOnly(2026, 02, 10),
                to: new DateOnly(2026, 02, 09),
                status: DocumentStatus.Draft,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("TaskSpan.To must be >= From.", ex.Message);
    }

    [Fact]
    public void UpsertTask_when_episode_closed_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 10));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.UpsertTask(Guid.NewGuid(), Guid.NewGuid(),
                from: new DateOnly(2026, 02, 05),
                to: null,
                status: DocumentStatus.Draft,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("Timesheet episode is closed.", ex.Message);
    }

    [Fact]
    public void UpsertTask_when_overlaps_with_other_active_span_should_throw_including_shared_day()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 12),
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.UpsertTask(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                from: new DateOnly(2026, 02, 12),
                to: new DateOnly(2026, 02, 15),
                status: DocumentStatus.Draft,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("Task spans overlap for the same person.", ex.Message);
    }

    [Fact]
    public void UpsertTask_when_overlaps_with_open_ended_active_span_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 10),
            to: null,
            status: DocumentStatus.Posted,
            author: "tester",
            nowUtc: NowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.UpsertTask(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                from: new DateOnly(2026, 02, 20),
                to: new DateOnly(2026, 02, 21),
                status: DocumentStatus.Draft,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("Task spans overlap for the same person.", ex.Message);
    }

    [Fact]
    public void UpsertTask_when_non_overlapping_should_allow_next_day()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 12),
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc);

        sut.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 13),
            to: new DateOnly(2026, 02, 15),
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc);

        Assert.Equal(2, sut.TaskSpans.Count);
    }

    [Fact]
    public void UpsertTask_when_overlaps_with_canceled_span_should_allow()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.TaskSpans.Add(new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = sut.Id,
            PersonId = sut.PersonId,
            CombatTaskDocumentId = Guid.NewGuid(),
            MissionId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 02, 10),
            ToDate = new DateOnly(2026, 02, 20),
            Status = DocumentStatus.Canceled,
            UpdatedBy = "tester",
            UpdatedAtUtc = NowUtc
        });

        sut.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 15),
            to: new DateOnly(2026, 02, 16),
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc);

        Assert.Equal(2, sut.TaskSpans.Count);
    }

    [Fact]
    public void UpsertTask_when_same_document_and_mission_should_update_in_place()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 12),
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc.AddMinutes(-1));

        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 11),
            to: new DateOnly(2026, 02, 13),
            status: DocumentStatus.Posted,
            author: "poster",
            nowUtc: NowUtc);

        Assert.Single(sut.TaskSpans);
        var span = sut.TaskSpans[0];
        Assert.Equal(new DateOnly(2026, 02, 11), span.FromDate);
        Assert.Equal(new DateOnly(2026, 02, 13), span.ToDate);
        Assert.Equal(DocumentStatus.Posted, span.Status);
        Assert.Equal("poster", span.UpdatedBy);
        Assert.Equal(NowUtc, span.UpdatedAtUtc);
    }

    // =========================
    // RemoveTaskForPerson
    // =========================

    [Fact]
    public void RemoveTaskForPerson_when_exists_should_remove()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: null,
            status: DocumentStatus.Draft,
            author: "tester",
            nowUtc: NowUtc);

        Assert.Single(sut.TaskSpans);

        sut.RemoveTaskForPerson(docId, missionId);

        Assert.Empty(sut.TaskSpans);
    }

    [Fact]
    public void RemoveTaskForPerson_when_missing_should_noop()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        sut.RemoveTaskForPerson(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(sut.TaskSpans);
    }

    // =========================
    // CloseTaskByReason
    // =========================

    [Fact]
    public void CloseTaskByReason_should_set_toDate_and_mark_span_canceled_and_set_reason_fields()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: null,
            status: DocumentStatus.Posted,
            author: "poster",
            nowUtc: NowUtc.AddMinutes(-5));

        var reasonId = Guid.NewGuid();
        sut.CloseTaskByReason(docId, missionId,
            closeAt: new DateOnly(2026, 02, 12),
            reasonCodeId: reasonId,
            reference: "  F200  ",
            author: "ops",
            nowUtc: NowUtc);

        var span = Assert.Single(sut.TaskSpans);
        Assert.Equal(new DateOnly(2026, 02, 12), span.ToDate);
        Assert.Equal(DocumentStatus.Canceled, span.Status);
        Assert.Equal(reasonId, span.ClosedByCodeId);
        Assert.Equal("F200", span.ClosedReference);
        Assert.Equal("ops", span.UpdatedBy);
        Assert.Equal(NowUtc, span.UpdatedAtUtc);
    }

    [Fact]
    public void CloseTaskByReason_when_toDate_is_before_closeAt_should_keep_existing_toDate()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: new DateOnly(2026, 02, 11),
            status: DocumentStatus.Posted,
            author: "poster",
            nowUtc: NowUtc.AddMinutes(-5));

        sut.CloseTaskByReason(docId, missionId,
            closeAt: new DateOnly(2026, 02, 12),
            reasonCodeId: Guid.NewGuid(),
            reference: null,
            author: "ops",
            nowUtc: NowUtc);

        var span = Assert.Single(sut.TaskSpans);
        Assert.Equal(new DateOnly(2026, 02, 11), span.ToDate);
        Assert.Equal(DocumentStatus.Canceled, span.Status);
        Assert.Null(span.ClosedReference);
    }

    [Fact]
    public void CloseTaskByReason_when_reference_is_whitespace_should_store_null()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        sut.UpsertTask(docId, missionId,
            from: new DateOnly(2026, 02, 10),
            to: null,
            status: DocumentStatus.Posted,
            author: "poster",
            nowUtc: NowUtc.AddMinutes(-5));

        sut.CloseTaskByReason(docId, missionId,
            closeAt: new DateOnly(2026, 02, 12),
            reasonCodeId: Guid.NewGuid(),
            reference: "   ",
            author: "ops",
            nowUtc: NowUtc);

        var span = Assert.Single(sut.TaskSpans);
        Assert.Null(span.ClosedReference);
    }

    [Fact]
    public void CloseTaskByReason_when_span_missing_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.CloseTaskByReason(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                closeAt: new DateOnly(2026, 02, 10),
                reasonCodeId: Guid.NewGuid(),
                reference: null,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("Task span not found.", ex.Message);
    }

    [Fact]
    public void CloseTaskByReason_when_episode_closed_should_throw()
    {
        var sut = NewEpisode(new DateOnly(2026, 02, 01), new DateOnly(2026, 02, 05));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.CloseTaskByReason(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                closeAt: new DateOnly(2026, 02, 03),
                reasonCodeId: Guid.NewGuid(),
                reference: null,
                author: "tester",
                nowUtc: NowUtc));

        Assert.Equal("Timesheet episode is closed.", ex.Message);
    }
}
