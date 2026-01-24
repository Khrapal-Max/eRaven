//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthPersonRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPersonMonthRowDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    IReadOnlyList<string> MainCodes,
    IReadOnlyList<string?> MainRef,
    IReadOnlyList<string> TaskCodes);
