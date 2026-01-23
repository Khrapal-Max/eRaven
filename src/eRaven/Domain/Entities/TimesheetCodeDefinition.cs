//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDefinition
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public sealed class TimesheetCodeDefinition
{
    public Guid Id { get; set; }

    public TimesheetLane Lane { get; set; }
    public string Code { get; set; } = string.Empty;   // "30", "НБ", "100"...
    public string Title { get; set; } = string.Empty;  // "В районі"...

    public TimesheetEndMode EndMode { get; set; } = TimesheetEndMode.PeriodOptional;

    public TimesheetEndDateMeaning EndDateMeaning { get; set; } = TimesheetEndDateMeaning.LastDayOfThisCode;

    /// <summary>
    /// Який код вважається "наступним" при FirstDayOfNextCode (зазвичай "30").
    /// Для Task можна лишати null.
    /// </summary>
    public string? NextCodeOnEnd { get; set; }
    public bool IsPlanningCutoff { get; set; }
    public int PlanningCutoffShiftDays { get; set; } = 1;
    public bool IsTerminal { get; set; }
    public bool RequiresReference { get; set; }
    public bool RequiresNote { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}