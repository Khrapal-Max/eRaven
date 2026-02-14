//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.CombatTask;

/// <summary>
/// Повертає список осіб, які були призначені на завдання в рамках певної місії на певну дату.
/// Включає як активні, так і видалені призначення, залежно від параметра IncludeDeleted.
/// 
/// <see cref="IsPlanned"/> визначає, чи враховувати заплановані завдання
/// (тобто ті, які ще не почалися, але вже призначені).
/// true - враховувати заплановані завдання, false - не враховувати.
/// </summary>
public sealed record GetCombatTaskMissionPersonsQuery(
    Guid MissionId,
    bool IsPlanned,
    DateOnly OnDate
);