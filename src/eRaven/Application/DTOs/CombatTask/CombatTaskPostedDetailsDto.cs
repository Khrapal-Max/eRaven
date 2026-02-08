//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPostedDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Плоский рядок для застосування в <see cref="MissionAssignment"/> при проведенні документа.
/// Містить все необхідне без навігацій.
/// </summary>
public sealed record CombatTaskPostedDetailsDto(
    Guid DocumentId,
    Guid CombatTaskId,
    Guid DetailsId,
    Guid MissionId,
    Guid PersonId,
    CombatTaskDetailsKind Kind,
    DateOnly EffectiveAt
);