//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.PersonInfo;

public sealed record ChangeWeaponCommand(
    Guid PersonId,
    DateOnly EffectiveDate,
    string? Weapon,
    string Author,
    DateTime NowUtc);