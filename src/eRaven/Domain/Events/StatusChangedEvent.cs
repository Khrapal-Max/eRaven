//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusChangedEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Подія зміни статусу (частина історії агрегату)
/// </summary>
public class StatusChangedEvent
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }

    public int StatusKindId { get; private set; }
    public DateTime EffectiveAt { get; private set; }
    public short Sequence { get; private set; }
    public string? Note { get; private set; }

    public string Author { get; private set; }
    public DateTime RecordedAt { get; private set; }

    public Guid? SourceDocumentId { get; private set; }
    public string? SourceDocumentType { get; private set; }

    private StatusChangedEvent()
    {
        Author = "system";
    }

    internal StatusChangedEvent(
        Guid personId,
        int statusKindId,
        DateTime effectiveAt,
        short sequence,
        string? note,
        string author,
        Guid? sourceDocumentId = null,
        string? sourceDocumentType = null)
    {
        Id = Guid.NewGuid();
        PersonId = personId;
        StatusKindId = statusKindId;
        EffectiveAt = effectiveAt;
        Sequence = sequence;
        Note = note?.Trim();
        Author = author;
        RecordedAt = DateTime.UtcNow;
        SourceDocumentId = sourceDocumentId;
        SourceDocumentType = sourceDocumentType;
    }
}