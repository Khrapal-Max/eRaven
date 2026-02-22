//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignment
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Матеріалізоване призначення на місію (read-модель CombatTask).
/// 
/// <para>
/// Відображає інтервал активності документа для конкретної особи в межах місії.
/// Інтервал має семантику half-open: <c>[From..To)</c>.
/// </para>
/// 
/// <para>
/// Примітка: це не "снапшот" особи — зберігаємо лише ідентифікатори + reference документа,
/// щоб оператор міг відкрити документ за номером.
/// </para>
/// </summary>
public sealed class MissionAssignment
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK: Person.</summary>
    public Guid PersonId { get; set; }

    /// <summary>FK: Mission.</summary>
    public Guid MissionId { get; set; }

    /// <summary>Start date (inclusive).</summary>
    public DateOnly From { get; set; }

    /// <summary>
    /// End date (exclusive).
    /// <para><c>null</c> means open-ended.</para>
    /// </summary>
    public DateOnly? To { get; set; }

    /// <summary>Документ-джерело, який відкрив інтервал.</summary>
    public Guid SourceStartDocumentId { get; set; }
    public Guid SourceStartDetailsId { get; set; }

    /// <summary>Документ-джерело, який закрив інтервал (опційно).</summary>
    public Guid? SourceEndDocumentId { get; set; }
    public Guid? SourceEndDetailsId { get; set; }

    // lightweight audit
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
