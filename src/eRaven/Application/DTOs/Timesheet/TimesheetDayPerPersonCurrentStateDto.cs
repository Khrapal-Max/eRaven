//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayCurrentStateDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetDayPerPersonCurrentStateDto
(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,

    Guid? MainEntryId,
    string MainCode,

    Guid? TaskEntryId,
    string TaskCode
);