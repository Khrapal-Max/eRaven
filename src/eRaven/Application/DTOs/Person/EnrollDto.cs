//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed class EnrollDto
{
    public Guid Id { get; set; }

    public EnrollmentKindDto Kind { get; set; } = EnrollmentKindDto.Unit;

    public string? Reference { get; set; } // опц.

    public string Reason { get; set; } = string.Empty;

    public DateOnly EnrollDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    // обов’язкові при зарахуванні (користувач має заповнити)
    public string Rank { get; set; } = string.Empty;
    public int PositionSort { get; set; } = 0;
    public string Position { get; set; } = string.Empty;
}
