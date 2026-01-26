//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskPlanDocumentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

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
/// - Assignment оновлюємо тільки на PostAsync().
/// - CancelAsync() дозволений тільки для Draft (бо Posted вже вплинув на стан).
/// - Порядок застосування: OrderBy(ActionDate) + End before Start на одну дату,
///   щоб дозволити "закрив і відкрив того ж дня".
/// </summary>
public interface ICombatTaskPlanDocumentRepository
{
    /// <summary>
    /// Створює Draft документ з рядками. Не змінює Assignments.
    /// Використовується UI для підготовки документу.
    /// </summary>
    Task<Guid> CreateDraftAsync(
        DateOnly recordedAt,
        DateOnly planningDate,
        string planningDocTitle,
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

    // (Read методи додамо пізніше під сторінки реєстру/плану, без "про запас".)
}
