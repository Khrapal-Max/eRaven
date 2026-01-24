//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryCreateDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// UI DTO for creating a timesheet entry (event) from month grid.
/// </summary>
public sealed class TimesheetEntryCreateDto
{
    public Guid PersonId { get; set; }
    public TimesheetLane Lane { get; set; }

    public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly? To { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public string Code { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Note { get; set; }
}
