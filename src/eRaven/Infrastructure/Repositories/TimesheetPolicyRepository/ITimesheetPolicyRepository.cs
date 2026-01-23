//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetCodeDefinition
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

public interface ITimesheetPolicyRepository
{
    Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(TimesheetLane lane, CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> GetAllowedNextAsync(Guid fromCodeId, CancellationToken ct = default);

    // Одна функція Save: повністю перезаписує дозволені переходи для fromCodeId
    Task SavePolicyAsync(
     TimesheetLane lane,
     Guid fromCodeId,
     TimesheetEndDateMeaning endDateMeaning,
     string? nextCodeOnEnd,
     IReadOnlyCollection<Guid> allowedToCodeIds,
     string author,
     DateTime nowUtc,
     CancellationToken ct = default);
}