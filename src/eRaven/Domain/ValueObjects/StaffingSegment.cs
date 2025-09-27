using eRaven.Domain.Models;

namespace eRaven.Domain.ValueObjects;

public sealed record StaffingSegment(
    Guid PersonId,
    TimeRange Range,
    StatusKind? Status,
    string? StatusNote,
    PositionUnit? PositionUnit,
    PlanAction? PlanAction = null)
{
    public bool HasStatus => Status is not null;

    public IEnumerable<TimeRange> SplitByDay() => Range.SplitByDay();
}
