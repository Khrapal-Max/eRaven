//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentDraftCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record CreateCombatTaskDocumentDraftCommand(
    string Title,
    DateOnly RecordedAt,
    string Author,
    DateTime NowUtc);
