//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonDayRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Запис для показу в строчці табличного представлення даних по людині за день
/// </summary>
public sealed record TimesheetPersonDayRowDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    TimesheetDayStateDto DayState);