//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonRangeRowDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Timesheets.Microservice;

public sealed record TimesheetPersonRangeRowDto(
    TimesheetPersonInfoDto Person,
    IReadOnlyList<TimesheetDaySnapshotDto> Days)
{
    public Guid PersonId => Person.PersonId;
    public string FullName => Person.FullName;
    public string Rnokpp => Person.Rnokpp;
    public string? Rank => Person.Rank;
    public string? Position => Person.Position;
    public EnrollmentKindDto EnrollmentKindDto => Person.EnrollmentKindDto;
    public DateOnly? EnrolledAt => Person.EnrolledAt;
    public DateOnly? ExcludedAt => Person.ExcludedAt;
}
