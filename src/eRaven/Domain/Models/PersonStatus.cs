//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatus
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Снапшот встановлення статусу людини.
/// </summary>
public sealed class PersonStatus
{
    public PersonStatus()
    {
        IsActive = true;
    }

    /// <summary>
    /// Ідентифікатор запису.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Людина, до якої належить статус.
    /// </summary>
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    /// <summary>
    /// Ідентифікатор статусу.
    /// </summary>
    public int StatusKindId { get; set; }
    public StatusKind StatusKind { get; set; } = default!;

    /// <summary>
    /// Час встановлення статусу.
    /// </summary>
    public DateTime OpenDate { get; set; }

    /// <summary>
    /// Чи активний статус.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Порядковий номер у черзі статусів.
    /// </summary>
    public short Sequence { get; set; }

    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime Modified { get; set; }

    // Аудит походження статусу
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentType { get; set; }

    /// <summary>
    /// Закриває статус.
    /// </summary>
    /// <param name="closedAtUtc">Час закриття.</param>
    /// <param name="author">Автор зміни.</param>
    /// <exception cref="InvalidOperationException">Статус уже закрито.</exception>
    internal void Close(DateTime closedAtUtc, string? author)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Статус вже закрито.");
        }

        IsActive = false;
        Author = author ?? Author;
        Modified = closedAtUtc;
    }

    /// <summary>
    /// Оновлює нотатку активного статусу.
    /// </summary>
    /// <param name="note">Новий коментар.</param>
    internal void UpdateNote(string? note, DateTime timestampUtc)
    {
        Note = note;
        Modified = timestampUtc;
    }

    /// <summary>
    /// Створює новий снапшот статусу.
    /// </summary>
    public static PersonStatus Create(
        Guid personId,
        int statusKindId,
        DateTime openDateUtc,
        short sequence,
        string? note,
        string? author,
        Guid? sourceDocumentId,
        string? sourceDocumentType)
    {
        var utc = openDateUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(openDateUtc, DateTimeKind.Utc)
            : openDateUtc.ToUniversalTime();

        return new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = statusKindId,
            OpenDate = utc,
            Sequence = sequence,
            Note = note,
            Author = author,
            SourceDocumentId = sourceDocumentId,
            SourceDocumentType = sourceDocumentType,
            Modified = utc,
            IsActive = true
        };
    }
}
