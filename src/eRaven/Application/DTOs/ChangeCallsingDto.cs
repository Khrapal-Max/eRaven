//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsingDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public class ChangeCallsingDto
{
    public Guid PersonId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Callsign { get; set; }
}
