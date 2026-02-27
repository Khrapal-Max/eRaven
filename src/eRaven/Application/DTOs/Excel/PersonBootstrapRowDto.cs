//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsBootstrapFileDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Excel;

public sealed record PersonBootstrapRowDto(
    int RowNumber,
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    EnrollmentKindDto Kind,
    string? Reference,
    DateOnly EnrollDate,
    string Reason,
    string Rank,
    int PositionSort,
    string Position,
    string? Bzvp,
    string? Weapon,
    string? Callsign
);
