//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipant
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

public class PlanParticipant
{
    public Guid Id { get; set; }

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = default!;

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    // 🔴 Снапшотні дані, щоб не губити історію при змінах Person
    public string FullName { get; set; } = default!;
    public string RankName { get; set; } = default!;
    public string PositionName { get; set; } = default!;
    public string UnitName { get; set; } = default!;

    /// <summary>Автор запису (аудит).</summary>
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    // 👇 Навігація до дій
    public ICollection<PlanParticipantAction> Actions { get; set; } = [];
}
