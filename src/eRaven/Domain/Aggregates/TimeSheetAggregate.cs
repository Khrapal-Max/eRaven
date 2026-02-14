//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Domain.Aggregates;

// Domain aggregate: root одного епізоду табеля (заміна TimesheetTimeline)
public sealed class TimesheetAggregate
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }

    public DateOnly OpenedAt { get; private set; }
    public DateOnly? ClosedAt { get; private set; }

    public string? Reason { get; private set; }

    /// <summary>Author who created the timeline.</summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>UTC timestamp when the timeline was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Author who closed the timeline (if any).</summary>
    public string? ClosedBy { get; set; }
    /// <summary>UTC timestamp when the timeline was closed (if any).</summary>
    public DateTime? ClosedAtUtc { get; set; }

    private readonly List<TimesheetEntry> _entries = [];
    public IReadOnlyList<TimesheetEntry> Entries => _entries;

    // --------------------
    // Lifecycle
    // --------------------
    public static TimesheetAggregate Open(Guid personId, DateOnly openedAt, string author, DateTime nowUtc)
    {
        return new TimesheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            CreatedBy = author,
            CreatedAtUtc = nowUtc
        };
    }

    public void Close(DateOnly closeTo, string? reason, string author, DateTime nowUtc)
    {
        ClosedAt = closeTo;
        Reason = reason;
        ClosedBy = author;
        ClosedAtUtc = nowUtc;
    }
}