//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsignCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record ChangeCallsignCommand(
    Guid PersonId,
    DateOnly EffectiveDate,
    string? Callsign,
    string Author,
    DateTime NowUtc);