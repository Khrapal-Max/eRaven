//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Domain.Aggregates;

public sealed class TimeSheetAggregate
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }

    public DateOnly OpenedAt { get; set; }
    public DateOnly? ClosedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? ClosedBy { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public List<TimesheetEntry> Entries { get; set; } = [];
    public List<TimesheetTaskSpan> TaskSpans { get; set; } = [];

    public bool IsClosed => ClosedAt.HasValue;

    public bool IsActiveOn(DateOnly d)
        => OpenedAt <= d && (!ClosedAt.HasValue || ClosedAt.Value >= d);

    public bool HasActiveTaskOn(DateOnly d)
        => TaskSpans.Any(s => s.IsActiveOn(d));

    public void EnsureNotClosed()
    {
        if (IsClosed)
            throw new InvalidOperationException("Timesheet episode is closed.");
    }

    public void EnsureInBounds(DateOnly d)
    {
        if (d < OpenedAt)
            throw new InvalidOperationException($"Date {d} is before OpenedAt {OpenedAt}.");

        if (ClosedAt.HasValue && d > ClosedAt.Value)
            throw new InvalidOperationException($"Date {d} is after ClosedAt {ClosedAt.Value}.");
    }

    public void UpsertTask(
        Guid documentId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        DocumentStatus status,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(from);
        if (to.HasValue) EnsureInBounds(to.Value);
        if (to.HasValue && to.Value < from) throw new InvalidOperationException("TaskSpan.To must be >= From.");

        // Забороняємо перетини між різними активними задачами (для цієї людини)
        foreach (var other in TaskSpans.Where(x => x.Status != DocumentStatus.Canceled))
        {
            // same span -> skip
            if (other.CombatTaskDocumentId == documentId && other.MissionId == missionId)
                continue;

            if (Overlaps(from, to, other.FromDate, other.ToDate))
                throw new InvalidOperationException("Task spans overlap for the same person.");
        }

        var span = TaskSpans.SingleOrDefault(x =>
            x.CombatTaskDocumentId == documentId &&
            x.MissionId == missionId);

        if (span is null)
        {
            span = new TimesheetTaskSpan
            {
                Id = Guid.NewGuid(),
                TimesheetId = Id,
                PersonId = PersonId, // <-- ВАЖЛИВО: денормалізований інваріант
                CombatTaskDocumentId = documentId,
                MissionId = missionId
            };
            TaskSpans.Add(span);
        }

        span.FromDate = from;
        span.ToDate = to;
        span.Status = status;
        span.UpdatedBy = author;
        span.UpdatedAtUtc = nowUtc;

        if (status != DocumentStatus.Canceled)
        {
            span.ClosedByCodeId = null;
            span.ClosedReference = null;
        }
    }

    public void RemoveTaskForPerson(Guid documentId, Guid missionId)
    {
        var span = TaskSpans.SingleOrDefault(x =>
            x.CombatTaskDocumentId == documentId &&
            x.MissionId == missionId);

        if (span is null) return;

        TaskSpans.Remove(span);
    }

    public void CloseTaskByReason(
        Guid documentId,
        Guid missionId,
        DateOnly closeAt,
        Guid reasonCodeId,
        string? reference,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(closeAt);

        var span = TaskSpans.SingleOrDefault(x =>
            x.CombatTaskDocumentId == documentId &&
            x.MissionId == missionId)
            ?? throw new InvalidOperationException("Task span not found.");

        if (!span.ToDate.HasValue || span.ToDate.Value > closeAt)
            span.ToDate = closeAt;

        span.Status = DocumentStatus.Canceled;
        span.ClosedByCodeId = reasonCodeId;
        span.ClosedReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        span.UpdatedBy = author;
        span.UpdatedAtUtc = nowUtc;
    }

    private static bool Overlaps(DateOnly aFrom, DateOnly? aTo, DateOnly bFrom, DateOnly? bTo)
    {
        var aEnd = aTo ?? DateOnly.MaxValue;
        var bEnd = bTo ?? DateOnly.MaxValue;
        return aFrom <= bEnd && bFrom <= aEnd;
    }
}