//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonEventCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonMove;

public sealed record VoidPersonEventCommand(
    Guid PersonId,
    Guid TargetEventId,
    string Reason,
    string Author,
    DateTime NowUtc);