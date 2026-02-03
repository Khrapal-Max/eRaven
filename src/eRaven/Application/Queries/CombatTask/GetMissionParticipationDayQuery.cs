//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionParticipationDayQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

/// <summary>
/// Повертає всіх осіб, які перебувають у місії на конкретну дату (overlap).
/// Може фільтрувати за MissionId і пошуком по snapshot (ПІБ/РНОКПП/позивний).
/// </summary>
public sealed record GetMissionParticipationDayQuery(
    DateOnly Date,
    Guid? MissionId = null,
    string? Search = null
);