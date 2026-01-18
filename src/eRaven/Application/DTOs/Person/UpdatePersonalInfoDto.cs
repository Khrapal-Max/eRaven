//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePersonalInfoDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public class UpdatePersonalInfoDto
{
    public Guid PersonId { get; set; }
    public string Rnokpp { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Note { get; set; }
}
