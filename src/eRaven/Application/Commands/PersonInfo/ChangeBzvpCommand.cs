//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PagedResult
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record ChangeBzvpCommand(
    Guid PersonId,
    DateOnly EffectiveDate,
    string Bzvp,
    string? Note,
    string Author,
    DateTime NowUtc);
