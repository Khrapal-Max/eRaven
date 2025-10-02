//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed class StatusKindDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int Order { get; init; }
    public bool IsActive { get; init; }
    public DateTime Modified { get; init; }
}