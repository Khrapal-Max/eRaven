//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDtoMapper
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Application.Mapper;

internal static class TimesheetDtoMapper
{
    public static EnrollmentKindDto MapEnrollmentKind(EnrollmentKind? kind)
        => kind switch
        {
            EnrollmentKind.Unit => EnrollmentKindDto.Unit,
            EnrollmentKind.AttachedByList => EnrollmentKindDto.AttachedByList,
            EnrollmentKind.AttachedByOrder => EnrollmentKindDto.AttachedByOrder,
            _ => EnrollmentKindDto.Unknown
        };

    public static TimesheetPersonInfoDto MapPerson(PersonReadModel p)
        => new(
            PersonId: p.Id,
            FullName: p.FullName,
            Rnokpp: p.Rnokpp,
            Rank: p.Rank,
            PositionSort: p.PositionSort,
            Position: p.Position,
            EnrollmentKindDto: MapEnrollmentKind(p.EnrollmentKind),
            EnrolledAt: p.EnrolledAt,
            ExcludedAt: p.ExcludedAt);

    public static TimesheetDaySnapshotDto MapDay(TimesheetDayRm d)
        => new(
            Date: d.DateOfDay,
            CodeId: d.CodeId,
            Code: d.Code,
            Reference: d.Reference,
            Note: d.Note,
            IsDerived: d.IsDerived,
            IsChangePoint: d.IsChangePoint,
            UiStyle: TimesheetEnumMapper.MapStyle(d.UiStyle));

    public static bool MatchesSearch(PersonReadModel p, string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var s = search.Trim();
        if (s.Length == 0) return true;

        return (p.FullName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
               || (p.Rnokpp?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
