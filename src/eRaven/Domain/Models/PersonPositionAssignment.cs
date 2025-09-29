//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPositionAssignment
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Снапшот призначення людини на посаду.
/// </summary>
public sealed class PersonPositionAssignment
{
    public PersonPositionAssignment()
    {
    }

    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public Guid PositionUnitId { get; set; }
    public PositionUnit PositionUnit { get; set; } = null!;

    public DateTime OpenUtc { get; set; }
    public DateTime? CloseUtc { get; set; }

    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime ModifiedUtc { get; set; }

    public bool IsActive => CloseUtc is null;

    internal void Close(DateTime closeUtc, string? note, string? author)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Призначення вже закрито.");
        }

        CloseUtc = closeUtc;
        Note = note ?? Note;
        Author = author ?? Author;
        ModifiedUtc = closeUtc;
    }

    internal void Touch(DateTime timestampUtc, string? note, string? author)
    {
        Note = note ?? Note;
        Author = author ?? Author;
        ModifiedUtc = timestampUtc;
    }

    public static PersonPositionAssignment Create(
        Guid personId,
        Guid positionUnitId,
        DateTime openUtc,
        string? note,
        string? author)
    {
        var utc = openUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(openUtc, DateTimeKind.Utc)
            : openUtc.ToUniversalTime();

        return new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = positionUnitId,
            OpenUtc = utc,
            Note = note,
            Author = author,
            ModifiedUtc = utc
        };
    }
}
