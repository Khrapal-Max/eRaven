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

    /// <summary>
    /// Зв'язок з людиною
    /// </summary>
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Поточний статус
    /// </summary>
    public int StatusKindId { get; set; }
    public StatusKind StatusKind { get; set; } = null!;

    /// <summary>
    /// ДІя з - до
    /// </summary>
    public DateTime OpenDate { get; set; }        // UTC

    /// <summary>
    /// Примітка
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Стан статусу - активний враховуємо для звітів/відмінений не враховуємо
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Автор
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Дата та час останбої зміни
    /// </summary>
    public DateTime Modified { get; set; }
}
