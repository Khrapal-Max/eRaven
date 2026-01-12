//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record ChangePositionCommand(
    Guid PersonId,
    DateOnly EffectiveDate,
    int? PositionSort,
    string? Position,
    string? Note,
    string Author,
    DateTime NowUtc);