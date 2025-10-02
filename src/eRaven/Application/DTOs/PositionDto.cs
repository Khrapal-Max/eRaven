//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed class PositionDto
{
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string ShortName { get; init; } = string.Empty;
    public string? OrgPath { get; init; }
    public string SpecialNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActived { get; init; }
    public string? CurrentPersonFullName { get; init; }
}