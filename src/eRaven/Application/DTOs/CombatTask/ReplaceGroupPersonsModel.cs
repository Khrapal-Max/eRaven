//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ReplaceGroupPersonsModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

public sealed record ReplaceGroupPersonsModel(
    Guid GroupId,
    IReadOnlyList<Guid> PersonIds);