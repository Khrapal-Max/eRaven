//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public class ChangeWeaponDto
{
    public Guid PersonId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Weapon { get; set; }
}
