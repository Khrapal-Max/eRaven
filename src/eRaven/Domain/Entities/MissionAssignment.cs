//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignment
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Інтервал призначення особи на місію (від Start до End).
/// Гарантія: на 1 особу може бути лише 1 "відкрите" призначення (EndedAt = NULL) у будь-який момент.
/// </summary>
public sealed class MissionAssignment
{
    public Guid Id { get; set; }

    public Guid MissionId { get; set; }

    public Guid PersonId { get; set; }

    public DateOnly StartedAt { get; set; }

    public DateOnly? EndedAt { get; set; }

    public Guid StartDocumentId { get; set; }

    public Guid? EndDocumentId { get; set; }

    public Guid StartActionId { get; set; }

    public Guid? EndActionId { get; set; }
}
