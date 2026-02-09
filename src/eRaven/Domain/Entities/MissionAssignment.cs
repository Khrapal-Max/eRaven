//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignment
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Факт участі особи у місії як інтервал (похідний/проєкційний запис з Posted документів).
/// </summary>
public sealed class MissionAssignment
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }
    public Guid MissionId { get; set; }

    public DateOnly From { get; set; }
    public DateOnly? To { get; set; }

    public MissionAssignmentStatus Status { get; set; } = MissionAssignmentStatus.Planned;

    /// <summary>Документ-джерело, який відкрив інтервал.</summary>
    public Guid SourceStartDocumentId { get; set; }
    public Guid SourceStartDetailsId { get; set; }

    /// <summary>Документ-джерело, який закрив інтервал (опційно).</summary>
    public Guid? SourceEndDocumentId { get; set; }
    public Guid? SourceEndDetailsId { get; set; }
}
