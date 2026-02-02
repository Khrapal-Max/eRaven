//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEntry
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Поточний обчислений стан особи відносно завдань (на дату).
/// </summary>
public sealed record CombatTaskPersonStateDto(
    Guid PersonId,
    bool IsOnTask,
    Guid? MissionId,
    string? MissionDisplaySnapshot,
    ActionKind? LastAction,
    DateOnly? LastActionDate,
    string? LastSourceDocNo
);
