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
    public string Rank { get; set; } = string.Empty;

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
    public string BZVP { get; set; } = string.Empty;

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
    public int? StatusKindId { get; set; }
    public StatusKind? StatusKind { get; set; }

    /// <summary>
    /// Придані/прибули
    /// </summary>
    public bool IsAttached { get; set; }               // свої=false, прибули=true
    public string? AttachedFromUnit { get; set; }      // назва/шлях підрозділу походження (для прибулих)

    /// <summary>
    /// Час створення
    /// </summary>
    public DateTime CreatedUtc { get; set; }           // UTC

    /// <summary>
    /// Час останньої зміни
    /// </summary>
    public DateTime ModifiedUtc { get; set; }

    /// <summary>
    /// Історія поточний статусів
    /// </summary>
    public ICollection<PersonStatus> StatusHistory { get; set; } = [];

    /// <summary>
    /// Історія поточний статусів
    /// </summary>
    public ICollection<PlanAction> PlanActions { get; set; } = [];

    /// <summary>
    /// Історія минулих посад
    /// </summary>
    public ICollection<PersonPositionAssignment> PositionAssignments { get; set; } = []; // історія

    /// <summary>
    /// Конкатенація повного імені
    /// </summary>
    public string FullName =>
        string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}