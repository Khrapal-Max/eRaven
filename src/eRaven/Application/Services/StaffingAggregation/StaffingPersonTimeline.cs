using eRaven.Domain.Models;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Services.StaffingAggregation;

public sealed record StaffingPersonTimeline(
    Person Person,
    IReadOnlyList<StaffingSegment> Segments,
    IReadOnlyList<PlanAction> PlanActions);
