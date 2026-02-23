//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignment
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Призначення особи на місію (інтервал зайнятості в домені CombatTask).
/// 
/// <para>
/// Відображає інтервал активності завдання для конкретної особи в межах місії.
/// Інтервал має семантику half-open: <c>[From..To)</c>.
/// </para>
/// 
/// <para>
/// Примітка: текстовий <c>reference</c> документа тут не зберігаємо — його можна отримати
/// через <see cref="SourceStartDocumentId"/> (join до <c>CombatTaskDocument.OrderTitle</c>)
/// при формуванні фактів для табеля/звітів.
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
