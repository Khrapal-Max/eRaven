//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs;

public sealed class EnrollDto
{
    public EnrollmentKind Kind { get; set; }
    public string? Reference { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Rank { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}
