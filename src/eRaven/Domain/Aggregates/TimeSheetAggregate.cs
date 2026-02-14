//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Domain.Aggregates;

public class TimeSheetAggregate
{
    public Guid Id { get; set; }

    //========================================
    // Person aggregate reference
    //========================================
    public Guid PersonID { get; set; }
    public PersonAggregate? Person { get; set; }

    public IReadOnlyList<TimesheetEntry> TimesheetEnties { get; set; } = [];

    public IReadOnlyList<CombatTaskDocument> CombatTaskDocuments { get; set; } = [];

    //========================================
    // Soft delete  
    //========================================
    public DateOnly CreatedAt { get; set; }
    public DateOnly ClosedAt { get; set; }
    public string? CloseReason { get; set; }

    // ============================
    // Commands
    // ============================
    public static void OpenOnEnroll(DateOnly enrollDate, string Author, DateOnly NowUtc)
    {
    }

    public void Excluded(DateOnly excludeDate, string reason, string Author, DateOnly NowUtc)
    {
        ClosedAt = excludeDate;
    }

    public void AddTimesheetEntry(TimesheetEntry entry)
    {
    }

    public void RemoveTimesheetEntry(Guid entryId)
    {
    }

    public void AddCombatTaskDocument(CombatTaskDocument document)
    {
    }

    public void RemoveCombatTaskDocument(Guid documentId)
    {
    }
}
