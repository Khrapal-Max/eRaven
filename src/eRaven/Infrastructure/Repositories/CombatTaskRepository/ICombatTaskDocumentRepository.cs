//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій документів планування бойових завдань.
/// 
/// Призначення:
/// - Документ = "пакет вводу" (один документ містить багато осіб/рядків Start/End).
/// - Документи мають статуси: Draft / Posted / Canceled.
/// - Саме через документ реалізується облік: користувач вводить/редагує Draft,
///   а проведення (Posted) робить зміни в CombatTaskAssignment.
/// 
/// Правило проведення:
/// - При Posted документі ми застосовуємо рядки:
///     Start => створити Assignment (якщо не існує відкритого)
///     End   => закрити існуючий Assignment
/// - В одному документі допускаємо кілька рядків на різних людей.
/// - Для однієї людини в одному документі: індекс (DocumentId, PersonId, Kind) забороняє дубль Start/End.
/// 
/// КРИТИЧНО:
/// - CancelAsync() дозволений тільки для Draft (бо Posted вже вплинув на стан).
/// </summary>
public interface ICombatTaskDocumentRepository
{
    /// <summary>
    /// Повертає список документів за місяць з фільтром пошуку по назві/номеру документа.
    /// Використовується UI для підготовки рєєстра. 
    /// </summary>
    Task<IReadOnlyList<CombatTaskDocumentDto>> GetDocumentsAsync(int year, int month, DocumentStatus? status, string? search, CancellationToken ct = default);

    /// <summary>
    /// Створює Draft документ з рядками. Не змінює Assignments.
    /// Використовується UI для підготовки документу.
    /// </summary>
    Task<Guid> CreateDraftAsync(
        string orderTitle,
        DateOnly recordedAt,
        string? description,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Проводить документ (Draft -> Posted) і застосовує рядки до Assignment-стану.
    /// Після цього документ стає джерелом аудиту: "хто/коли/чим відкрив/закрив".
    /// </summary>
    Task PostAsync(Guid documentId, string author, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Відміняє Draft документ (Draft -> Canceled). Posted відміняти не можна.
    /// Відмінені документи не показуємо у плані/звітності.
    /// </summary>
    Task CancelAsync(Guid documentId, string reason, string author, DateTime nowUtc, CancellationToken ct = default);
}