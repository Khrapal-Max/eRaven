//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public class ChangeRankDto
{
    public Guid PersonId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Rank { get; set; } = string.Empty;
    public string? Note { get; set; }
}
