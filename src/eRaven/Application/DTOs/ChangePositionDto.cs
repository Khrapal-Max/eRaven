//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs;

public class ChangePositionDto
{
    public Guid PersonId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public int? PositionSort { get; set; }
    public string? Position { get; set; }
    public string? Note { get; set; }
}
