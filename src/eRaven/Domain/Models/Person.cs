//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Людина, основа для картки
/// </summary>
public class Person
{
    public Guid Id { get; set; }

    /// <summary>
    /// ІПН
    /// </summary>
    public string Rnokpp { get; set; } = string.Empty;

    /// <summary>
    /// Звання
    /// </summary>
    public string? Rank { get; set; }

    /// <summary>
    /// Прізвище
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Ім'я
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// По батькові
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Наявність юазовго навчанян
    /// </summary>
    public string? BZVP { get; set; }

    /// <summary>
    /// Тип та номер зброї
    /// </summary>
    public string? Weapon { get; set; }

    /// <summary>
    /// Позивний
    /// </summary>
    public string? Callsign { get; set; }

    /// <summary>
    /// Посада в штатному розкладі
    /// </summary>
    public Guid? PositionUnitId { get; set; }
    public PositionUnit? PositionUnit { get; set; }

    /// <summary>
    /// Поточний статус - наприклад "В районі" або "СЗЧ"
    /// </summary>
    public int StatusKindId { get; set; }
    public StatusKind StatusKind { get; set; } = null!;

    /// <summary>
    /// Історія поточний статусів
    /// </summary>
    public ICollection<PersonStatus> StatusHistory { get; set; } = [];

    /// <summary>
    /// Конкатенація повного імені
    /// </summary>
    public string FullName =>
        string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}