//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidEventDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public sealed class VoidPersonEventDto
{
    public Guid PersonId { get; set; }
    public Guid TargetEventId { get; set; }
    public string? Reason { get; set; }
}