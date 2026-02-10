//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PostCombatTaskDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

/// <summary>
/// Проводе документ в стан posted
/// </summary>
public sealed record PostCombatTaskDocumentCommand(
    Guid DocumentId,
    string Author,
    DateTime NowUtc
);