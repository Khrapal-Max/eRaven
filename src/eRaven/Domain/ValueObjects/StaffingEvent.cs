using eRaven.Domain.Models;

namespace eRaven.Domain.ValueObjects;

public sealed record StaffingEvent(
    Guid PersonId,
    DateTime EffectiveAtUtc,
    StaffingEventKind Kind,
    StatusKind? StatusKind = null,
    string? StatusNote = null,
    PositionUnit? PositionUnit = null,
    PlanAction? PlanAction = null,
    short? Sequence = null)
{
    public int Priority => Kind switch
    {
        StaffingEventKind.Baseline => 0,
        StaffingEventKind.StatusChanged => 10,
        StaffingEventKind.StatusCleared => 11,
        StaffingEventKind.PositionReleased => 20,
        StaffingEventKind.PositionAssigned => 21,
        StaffingEventKind.PlanAction => 30,
        _ => 100
    };

    public static StaffingEvent CreateBaseline(Guid personId, DateTime effectiveAtUtc, StatusKind? statusKind, string? statusNote, PositionUnit? positionUnit)
        => new(personId, EnsureUtc(effectiveAtUtc), StaffingEventKind.Baseline, statusKind, statusNote, positionUnit);

    public static StaffingEvent CreateStatusChanged(PersonStatus status)
        => new(
            status.PersonId,
            EnsureUtc(status.OpenDate),
            StaffingEventKind.StatusChanged,
            status.StatusKind,
            status.Note,
            Sequence: status.Sequence);

    public static StaffingEvent CreateStatusCleared(Guid personId, DateTime effectiveAtUtc)
        => new(personId, EnsureUtc(effectiveAtUtc), StaffingEventKind.StatusCleared);

    public static StaffingEvent CreatePositionAssigned(PersonPositionAssignment assignment)
        => new(
            assignment.PersonId,
            EnsureUtc(assignment.OpenUtc),
            StaffingEventKind.PositionAssigned,
            PositionUnit: assignment.PositionUnit);

    public static StaffingEvent CreatePositionReleased(Guid personId, DateTime effectiveAtUtc)
        => new(personId, EnsureUtc(effectiveAtUtc), StaffingEventKind.PositionReleased);

    public static StaffingEvent CreatePlanAction(PlanAction plan)
        => new(plan.PersonId, EnsureUtc(plan.EffectiveAtUtc), StaffingEventKind.PlanAction, PlanAction: plan);

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
