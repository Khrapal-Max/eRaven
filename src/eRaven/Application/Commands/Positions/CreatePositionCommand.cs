//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Positions;

public sealed record CreatePositionCommand(
    string? Code,
    string ShortName,
    string SpecialNumber,
    string OrgPath
);