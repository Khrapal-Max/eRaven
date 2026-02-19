//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Application.Abstractions.CombatTaskRepository;

/// <summary>
/// Репозиторій документів бойових завдань.
///
/// <para>Спрощена модель:</para>
/// <list type="bullet">
/// <item><description>Документ одразу є чинним (Active) та формує факт у табелі.</description></item>
/// <item><description>Чернеток/Posted немає.</description></item>
/// <item><description>Факт не видаляємо — тільки компенсація через Cancel.</description></item>
/// </list>
/// </summary>
public interface ICombatTaskDocumentRepository
{
    /// <summary>
    /// Повертає список документів із фільтрами.
    /// </summary>
    Task<IReadOnlyList<CombatTaskDocument>> GetDocumentsAsync(
        int? year,
        int? month,
        DocumentStatus? status,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Створює новий документ у стані <c>Active</c>.
    /// </summary>
    Task<Guid> CreateAsync(
        string orderTitle,
        DateOnly recordedAt,
        string? description,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Скасовує (Void/Cancel) документ через компенсацію.
    /// </summary>
    Task CancelAsync(
        Guid documentId,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
