//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetDayQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries.Timesheet;

public sealed record GetTimesheetDayQuery(
     DateOnly Date,
     string? Search = null,
     EnrollmentKind? EnrollmentKind = null,
     bool ActiveOnly = true
);
