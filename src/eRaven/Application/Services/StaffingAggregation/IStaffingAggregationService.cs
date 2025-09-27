using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Services.StaffingAggregation;

public interface IStaffingAggregationService
{
    Task<IReadOnlyList<StaffingPersonTimeline>> BuildTimelineAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
