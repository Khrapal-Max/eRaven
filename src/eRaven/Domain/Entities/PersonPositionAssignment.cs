//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Запис призначення на посаду
/// </summary>
public class PersonPositionAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// Людина
    /// </summary>
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Посада
    /// </summary>
    public Guid PositionUnitId { get; set; }
    public PositionUnit PositionUnit { get; set; } = null!;

    /// <summary>
    /// Тривалість на посаді
    /// </summary>
    public DateTime OpenUtc { get; set; }
    public DateTime? CloseUtc { get; set; } // null = активне закріплення

    /// <summary>
    /// Нотатка
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Автор
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Час запису
    /// </summary>
    public DateTime ModifiedUtc { get; set; }
}
