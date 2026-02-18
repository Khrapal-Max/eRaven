//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Domain.Aggregates;

/// <summary>
/// Епізод табеля (episode) з подіями та фактами задач (TaskSpans).
///
/// <para><b>Семантика дат для TaskSpans:</b></para>
/// <list type="bullet">
///   <item><description><c>FromDate</c> — інклюзивна дата початку.</description></item>
///   <item><description><c>ToDate</c> — <b>EXCLUSIVE</b> (half-open інтервал <c>[From..To)</c>).</description></item>
///   <item><description><c>ToDate = null</c> — інтервал відкритий в майбутнє.</description></item>
/// </list>
/// </summary>
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

    private void EnsureTaskToInBounds(DateOnly toExclusive)
    {
        // toExclusive може дорівнювати ClosedAt+1, щоб покрити останній день ClosedAt.
        if (toExclusive < OpenedAt)
            throw new InvalidOperationException($"Task ToDate {toExclusive} is before OpenedAt {OpenedAt}.");

        if (ClosedAt.HasValue && toExclusive > ClosedAt.Value.AddDays(1))
            throw new InvalidOperationException($"Task ToDate {toExclusive} is after ClosedAt+1 {ClosedAt.Value.AddDays(1)}.");
    }

    public void UpsertTask(
    Guid documentId,
    Guid missionId,
    DateOnly from,
    DateOnly? toExclusive,
    string? rnokpp,
    string? fullName,
    string? rank,
    string? position,
    string? weapon,
    string? callsign,
    string? openedByDocumentReference,
    string author,
    DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureInBounds(from);

        if (toExclusive.HasValue)
        {
            EnsureTaskToInBounds(toExclusive.Value);
            if (toExclusive.Value < from)
                throw new InvalidOperationException("TaskSpan.ToDate (exclusive) must be >= FromDate.");
        }

        foreach (var other in TaskSpans.Where(x => x.Status != DocumentStatus.Canceled))
        {
            if (other.OpenedByCombatTaskDocumentId == documentId && other.MissionId == missionId)
                continue;

            if (Overlaps(from, toExclusive, other.FromDate, other.ToDate))
                throw new InvalidOperationException("Task spans overlap for the same person.");
        }

        var span = TaskSpans.SingleOrDefault(x =>
            x.OpenedByCombatTaskDocumentId == documentId &&
            x.MissionId == missionId);

        if (span is null)
        {
            span = new TimesheetTaskSpan
            {
                Id = Guid.NewGuid(),
                TimesheetId = Id,
                PersonId = PersonId,
                OpenedByCombatTaskDocumentId = documentId,
                MissionId = missionId,
                Status = DocumentStatus.Active,
                CreatedBy = author,
                CreatedAtUtc = nowUtc
            };
            TaskSpans.Add(span);
        }

        span.FromDate = from;
        span.ToDate = toExclusive;


        span.OpenedByDocumentReference = string.IsNullOrWhiteSpace(openedByDocumentReference)
            ? null : openedByDocumentReference.Trim();

        span.Rnokpp = (rnokpp ?? string.Empty).Trim();
        span.FullName = (fullName ?? string.Empty).Trim();
        span.Rank = string.IsNullOrWhiteSpace(rank) ? null : rank.Trim();
        span.Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim();
        span.Weapon = string.IsNullOrWhiteSpace(weapon) ? null : weapon.Trim();
        span.Callsign = string.IsNullOrWhiteSpace(callsign) ? null : callsign.Trim();

        span.UpdatedBy = author;
        span.UpdatedAtUtc = nowUtc;

        // (re)open markers
        span.Status = DocumentStatus.Active;
        span.ClosedByCombatTaskDocumentId = null;
        span.ClosedByCodeId = null;
        span.ClosedReference = null;
        span.ClosedByDocumentReference = null;
    }

    /// <summary>
    /// Завершує активний факт задачі документом <paramref name="closingDocumentId"/>.
    /// Дозволяє кейс: документ №1 відкрив задачу, документ №2 закрив.
    ///
    /// <para><b>Важливо:</b> <paramref name="closeAtExclusive"/> — EXCLUSIVE дата закриття.</para>
    /// <para>Напр.: якщо останній день задачі = 2026-02-12 (inclusive), то closeAtExclusive = 2026-02-13.</para>
    ///
    /// <para>Також оновлює snapshot (ПІБ/звання/посада/зброя/позивний) даними документа, що закриває.</para>
    /// </summary>
    public void CloseTaskByDocument(
    Guid closingDocumentId,
    Guid missionId,
    DateOnly closeAtExclusive,
    string rnokpp,
    string fullName,
    string? rank,
    string? position,
    string? weapon,
    string? callsign,
    string? closedByDocumentReference,
    string author,
    DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureTaskToInBounds(closeAtExclusive);

        var lastDay = closeAtExclusive.AddDays(-1);
        EnsureInBounds(lastDay);

        var span = TaskSpans
            .Where(x => x.MissionId == missionId)
            .Where(x => x.Status != DocumentStatus.Canceled)
            .SingleOrDefault(x => x.IsActiveOn(lastDay))
            ?? throw new InvalidOperationException("Active task span not found.");

        if (!span.ToDate.HasValue || span.ToDate.Value > closeAtExclusive)
            span.ToDate = closeAtExclusive;

        span.ClosedByCombatTaskDocumentId = closingDocumentId;

        // ✅ NEW: snapshot референса документа, що закрив
        span.ClosedByDocumentReference = string.IsNullOrWhiteSpace(closedByDocumentReference)
            ? null
            : closedByDocumentReference.Trim();

        // Документне закриття не є "закриттям по коду"
        span.ClosedByCodeId = null;
        span.ClosedReference = null;

        // Snapshot з документа, що закриває
        span.Rnokpp = (rnokpp ?? string.Empty).Trim();
        span.FullName = (fullName ?? string.Empty).Trim();
        span.Rank = string.IsNullOrWhiteSpace(rank) ? null : rank.Trim();
        span.Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim();
        span.Weapon = string.IsNullOrWhiteSpace(weapon) ? null : weapon.Trim();
        span.Callsign = string.IsNullOrWhiteSpace(callsign) ? null : callsign.Trim();

        span.UpdatedBy = author;
        span.UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Закриває всі активні task-span’и <b>на останній активний день</b> перед <paramref name="closeAtExclusive"/>
    /// та проставляє причину (код/референс).
    ///
    /// <para><b>Важливо:</b> <paramref name="closeAtExclusive"/> — EXCLUSIVE дата.</para>
    /// <para>Для пошуку "активних" span’ів використовується дата <c>closeAtExclusive - 1 день</c>.</para>
    /// </summary>
    public void CloseTaskByReason(
        DateOnly closeAtExclusive,
        Guid reasonCodeId,
        string? reference,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();
        EnsureTaskToInBounds(closeAtExclusive);

        var lastDay = closeAtExclusive.AddDays(-1);
        EnsureInBounds(lastDay);

        var spans = TaskSpans
            .Where(x => x.Status != DocumentStatus.Canceled)
            .Where(x => x.IsActiveOn(lastDay))
            .ToList();

        if (spans.Count == 0)
            throw new InvalidOperationException("Active task span not found.");

        var trimmedRef = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();

        foreach (var span in spans)
        {
            if (!span.ToDate.HasValue || span.ToDate.Value > closeAtExclusive)
                span.ToDate = closeAtExclusive;

            span.ClosedByCodeId = reasonCodeId;
            span.ClosedReference = trimmedRef;

            // Закриття по причині не є "закриттям документом"
            span.ClosedByCombatTaskDocumentId = null;
            span.ClosedByDocumentReference = null;

            span.UpdatedBy = author;
            span.UpdatedAtUtc = nowUtc;
        }
    }

    public void CancelTask(
        Guid documentId,
        Guid missionId,
        Guid reasonCodeId,
        string reference,
        string author,
        DateTime nowUtc)
    {
        EnsureNotClosed();

        var span = TaskSpans.SingleOrDefault(x =>
                x.MissionId == missionId
                && (x.OpenedByCombatTaskDocumentId == documentId
                    || x.ClosedByCombatTaskDocumentId == documentId))
            ?? throw new InvalidOperationException("Task span not found.");

        span.Status = DocumentStatus.Canceled;

        span.ClosedByCodeId = reasonCodeId;
        span.ClosedReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();

        span.UpdatedBy = author;
        span.UpdatedAtUtc = nowUtc;
    }

    private static bool Overlaps(DateOnly aFrom, DateOnly? aToExclusive, DateOnly bFrom, DateOnly? bToExclusive)
    {
        var aEnd = aToExclusive ?? DateOnly.MaxValue;
        var bEnd = bToExclusive ?? DateOnly.MaxValue;
        return aFrom < bEnd && bFrom < aEnd;
    }
}
