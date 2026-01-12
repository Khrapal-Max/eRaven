//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonMove;

public sealed record ExcludeCommand(
    Guid PersonId,
    string Reason,
    DateOnly EffectiveDate,
    string Author,
    DateTime NowUtc);