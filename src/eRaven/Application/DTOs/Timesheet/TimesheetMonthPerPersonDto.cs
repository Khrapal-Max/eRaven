//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthPerPersonDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetMonthPerPersonDto
(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,     // <-- додали
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    MonthlyTimesheetReadModelDto? Timesheet);
