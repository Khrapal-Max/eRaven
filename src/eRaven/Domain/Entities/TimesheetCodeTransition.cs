//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeTransition
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Allowed transition between codes.
/// Example: From "100" -> To "30".
/// </summary>
public sealed class TimesheetCodeTransition
{
    public Guid Id { get; set; }

    public Guid FromCodeId { get; set; }
    public TimesheetCodeDefinition FromCode { get; set; } = null!;

    public Guid ToCodeId { get; set; }
    public TimesheetCodeDefinition ToCode { get; set; } = null!;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
