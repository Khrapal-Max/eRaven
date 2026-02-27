//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonInfoDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Єдиний Person snapshot для табеля (UI).
/// <para>
/// <see cref="PositionSort"/> потрібен для сортування по посадам.
/// </para>
/// </summary>
public sealed record TimesheetPersonInfoDto(
    Guid PersonId,
    string FullName,
    string Rnokpp,
    string? Rank,
    int? PositionSort,
    string? Position,
    EnrollmentKindDto EnrollmentKindDto,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt);
