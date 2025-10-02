//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAssignedToPositionEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Подія призначення на посаду (частина історії агрегату)
/// </summary>
public class PositionAssignedEvent
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid PositionUnitId { get; private set; }

    public DateTime OpenUtc { get; private set; }
    public DateTime? CloseUtc { get; private set; }
    public string? Note { get; private set; }

    public string Author { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private PositionAssignedEvent()
    {
        Author = "system";
    }

    internal PositionAssignedEvent(
        Guid personId,
        Guid positionUnitId,
        DateTime openUtc,
        string? note,
        string author)
    {
        Id = Guid.NewGuid();
        PersonId = personId;
        PositionUnitId = positionUnitId;
        OpenUtc = openUtc;
        CloseUtc = null;
        Note = note?.Trim();
        Author = author;
        RecordedAt = DateTime.UtcNow;
    }

    internal void Close(DateTime closeUtc, string? note)
    {
        if (CloseUtc.HasValue)
            throw new InvalidOperationException("Призначення вже закрито");

        if (closeUtc <= OpenUtc)
            throw new ArgumentException("Дата закриття має бути пізніше дати відкриття");

        CloseUtc = closeUtc;
        if (!string.IsNullOrWhiteSpace(note))
            Note = note.Trim();
    }
}