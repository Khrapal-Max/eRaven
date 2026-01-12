//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record ChangeRankCommand(
    Guid PersonId,
    DateOnly EffectiveDate,
    string Rank,
    string? Note,
    string Author,
    DateTime NowUtc);