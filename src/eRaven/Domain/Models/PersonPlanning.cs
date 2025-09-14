// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PersonPlanning (read-model для швидких гвардів планування; 1:1 з Person)
// -----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

/// <summary>
/// Денормалізований стан планування по особі (оновлюється транзакційно разом із PlanElement).
/// Не є джерелом історії, лише швидкий індекс для гвардів та підбору кандидатів.
/// </summary>
public class PersonPlanning
{
    public Guid Id { get; set; }

    /// <summary>FK на Person (унікально; 1:1).</summary>
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    // ---- Поточний операційний стан (для фільтру "можна відрядити") ----
    public int? CurrentStatusKindId { get; set; }
    public string? CurrentStatusKindCode { get; set; }

    // ---- Остання фактична планова дія (історично) ----
    public PlanType? LastActionType { get; set; }
    public DateTime? LastActionAtUtc { get; set; }
    public string? OpenLocation { get; set; }
    public string? OpenGroup { get; set; }
    public string? OpenTool { get; set; }

    // ---- Аудит ----
    public string? Author { get; set; } = "system";
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
}
