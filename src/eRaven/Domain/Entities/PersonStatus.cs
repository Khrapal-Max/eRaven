//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatus
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Статус людини
/// </summary>
public class PersonStatus
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = default!;

    public int StatusKindId { get; set; }
    public StatusKind StatusKind { get; set; } = default!;

    public DateTime OpenDate { get; set; }
    public bool IsActive { get; set; } = true;
    public short Sequence { get; set; }
    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime Modified { get; set; }

    // НОВЕ: для аудиту, звідки з’явився цей статус
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentType { get; set; }
}
