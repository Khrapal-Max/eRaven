//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentDraftCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

public sealed record CreateCombatTaskDocumentDraftCommand(
    string OrderTitle,
    DateOnly RecordedAt,
    string? Description,
    string Author,
    DateTime NowUtc);
