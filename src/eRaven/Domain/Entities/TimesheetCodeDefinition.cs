//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDefinition
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Dictionary entry for available timesheet codes.
/// Example: Code="30", Title="В районі".
/// </summary>
public sealed class TimesheetCodeDefinition
{
    public Guid Id { get; set; }

    /// <summary>Stable code used in entries/reports ("30", "НБ", "100"...).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human readable title ("В районі"...).</summary>
    public string Title { get; set; } = string.Empty;

    public TimesheetEndMode EndMode { get; set; } = TimesheetEndMode.PeriodOptional;
    public TimesheetEndDateMeaning EndDateMeaning { get; set; } = TimesheetEndDateMeaning.LastDayOfThisCode;

    /// <summary>
    /// Code considered as “next” when EndDateMeaning == FirstDayOfNextCode (often "30").
    /// For task-like codes can be null.
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
