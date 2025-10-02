//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.PlanActions;

public sealed record CreatePlanActionCommand(
    Guid PersonId,
    string PlanActionName,
    DateTime EffectiveAtUtc,
    MoveType MoveType,
    string Location,
    string? GroupName = null,
    string? CrewName = null,
    string? Note = null
);