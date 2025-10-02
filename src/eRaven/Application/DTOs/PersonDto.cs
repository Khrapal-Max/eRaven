//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed class PersonDto
{
    public Guid Id { get; init; }
    public string Rnokpp { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string FullName { get; init; } = string.Empty;

    public string Rank { get; init; } = string.Empty;
    public string BZVP { get; init; } = string.Empty;
    public string? Weapon { get; init; }
    public string? Callsign { get; init; }

    public int? StatusKindId { get; init; }
    public string? StatusKindName { get; init; }
    public string? StatusKindCode { get; init; }

    public Guid? PositionUnitId { get; init; }
    public string? PositionUnitName { get; init; }

    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}