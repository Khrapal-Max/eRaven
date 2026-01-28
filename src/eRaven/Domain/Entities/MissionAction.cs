//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public class MissionAction
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Sequence { get; set; }

    public ActionKind Action {  get; set; }

    public Guid MissionId { get; set; }

    public Guid PersonId { get; set; }

    public DateOnly ActionDate { get; set; }
}
