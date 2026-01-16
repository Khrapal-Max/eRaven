//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeBzvpDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public class ChangeBzvpDto
{
    public Guid PersonId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Bzvp { get; set; } = string.Empty;
    public string? Note { get; set; }
}
