//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionParticipationMonthQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

/// <summary>
/// Повертає всі участі, що перетинають місяць (overlap).
/// Підходить для month-report: хто/яка місія/з яких дат.
/// </summary>
public sealed record GetMissionParticipationMonthQuery(
    int Year,
    int Month,
    Guid? MissionId = null,
    string? Search = null
);