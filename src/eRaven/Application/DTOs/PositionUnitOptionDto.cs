//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitOptionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed record PositionUnitOptionDto(
    Guid Id,
    string Code,
    string ShortName,
    string FullName,
    string Rank,
    string Tarif
);
