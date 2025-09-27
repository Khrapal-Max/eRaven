namespace eRaven.Domain.ValueObjects;

public enum StaffingEventKind
{
    Baseline = 0,
    StatusChanged = 1,
    StatusCleared = 2,
    PositionAssigned = 3,
    PositionReleased = 4,
    PlanAction = 5
}
