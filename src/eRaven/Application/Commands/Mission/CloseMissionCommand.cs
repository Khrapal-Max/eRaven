//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseMissionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Mission;

/// <summary>
/// Закриває місію (ClosedAt).
/// Ідемпотентно: якщо вже закрита — нічого не робить (за поточним repo).
/// </summary>
public sealed record CloseMissionCommand(
    Guid MissionId,
    DateOnly ClosedAt
);
