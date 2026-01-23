//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeTransition
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public sealed class TimesheetCodeTransition
{
    public Guid Id { get; set; }

    public TimesheetLane Lane { get; set; }

    public Guid FromCodeId { get; set; }
    public TimesheetCodeDefinition FromCode { get; set; } = null!;

    public Guid ToCodeId { get; set; }
    public TimesheetCodeDefinition ToCode { get; set; } = null!;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}