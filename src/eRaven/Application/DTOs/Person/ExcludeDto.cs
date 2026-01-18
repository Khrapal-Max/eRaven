//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public sealed class ExcludeDto
{
    public Guid Id { get; set; }
    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Reason { get; set; } = string.Empty;
}
