//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// MissionAssignmentStatus
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

/// <summary>
/// Статус проєкційного інтервалу участі у місії.
/// Planned  — запис створений з Draft (план/оперативна картина),
/// Committed — підтверджено (проведено / зафіксовано),
/// Voided/Corrected — резерв під подальші сценарії (не обов’язково використовувати зараз).
/// </summary>
public enum MissionAssignmentStatus
{
    Planned = 0,
    Committed = 1,
    Voided = 2,
    Corrected = 3
}
