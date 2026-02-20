//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonInfoDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheets.Microservice;

/// <summary>
/// Єдиний Person snapshot для табеля.
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
