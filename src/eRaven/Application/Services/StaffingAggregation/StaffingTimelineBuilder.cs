using eRaven.Domain.Models;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Services.StaffingAggregation;

internal sealed class StaffingTimelineBuilder(DateTime fromUtc, DateTime toUtc)
{
    private readonly DateTime _fromUtc = EnsureUtc(fromUtc);
    private readonly DateTime _toUtc = EnsureUtc(toUtc);

    public IReadOnlyList<StaffingSegment> Build(Guid personId, IEnumerable<StaffingEvent> events)
    {
        if (_fromUtc >= _toUtc)
            return Array.Empty<StaffingSegment>();

        var ordered = events
            .Where(e => e.PersonId == personId && e.EffectiveAtUtc < _toUtc)
            .Select(Normalize)
            .DistinctBy(e => (e.PersonId, e.Kind, e.EffectiveAtUtc, e.StatusKind?.Id, e.PositionUnit?.Id, e.StatusNote, e.PlanAction?.Id, e.Sequence))
            .OrderBy(e => e.EffectiveAtUtc)
            .ThenBy(e => e.Priority)
            .ThenBy(e => e.Sequence ?? 0)
            .ToList();

        if (ordered.Count == 0)
            return Array.Empty<StaffingSegment>();

        var segments = new List<StaffingSegment>();
        var state = new StaffingState();
        var cursor = _fromUtc;

        foreach (var evt in ordered)
        {
            if (evt.EffectiveAtUtc <= _fromUtc)
            {
                Apply(evt, state);
                continue;
            }

            var segmentEnd = evt.EffectiveAtUtc < _toUtc ? evt.EffectiveAtUtc : _toUtc;
            if (segmentEnd > cursor && state.HasStatus)
            {
                var range = new TimeRange(cursor, segmentEnd);
                segments.Add(new StaffingSegment(personId, range, state.Status, state.StatusNote, state.PositionUnit, state.PlanAction));
            }

            Apply(evt, state);

            if (evt.EffectiveAtUtc > cursor)
                cursor = evt.EffectiveAtUtc;

            if (cursor >= _toUtc)
                break;
        }

        if (cursor < _toUtc && state.HasStatus)
        {
            var range = new TimeRange(cursor, _toUtc);
            segments.Add(new StaffingSegment(personId, range, state.Status, state.StatusNote, state.PositionUnit, state.PlanAction));
        }

        return segments;
    }

    private static StaffingEvent Normalize(StaffingEvent evt)
    {
        if (evt.EffectiveAtUtc.Kind != DateTimeKind.Utc)
        {
            evt = evt with { EffectiveAtUtc = DateTime.SpecifyKind(evt.EffectiveAtUtc, DateTimeKind.Utc) };
        }

        return evt;
    }

    private static void Apply(StaffingEvent evt, StaffingState state)
    {
        switch (evt.Kind)
        {
            case StaffingEventKind.Baseline:
            case StaffingEventKind.StatusChanged:
                state.Status = evt.StatusKind;
                state.StatusNote = string.IsNullOrWhiteSpace(evt.StatusNote) ? null : evt.StatusNote.Trim();
                if (evt.PositionUnit is not null)
                    state.PositionUnit = evt.PositionUnit;
                break;
            case StaffingEventKind.StatusCleared:
                state.Status = null;
                state.StatusNote = null;
                break;
            case StaffingEventKind.PositionAssigned:
                state.PositionUnit = evt.PositionUnit;
                break;
            case StaffingEventKind.PositionReleased:
                state.PositionUnit = null;
                break;
            case StaffingEventKind.PlanAction:
                state.PlanAction = evt.PlanAction;
                break;
        }
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class StaffingState
    {
        public StatusKind? Status { get; set; }
        public string? StatusNote { get; set; }
        public PositionUnit? PositionUnit { get; set; }
        public PlanAction? PlanAction { get; set; }

        public bool HasStatus => Status is not null;
    }
}
