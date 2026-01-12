//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public sealed class CreateReservedDto
{
    public string Rnokpp { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public string? Rank { get; set; }
    public string? Position { get; set; }
}