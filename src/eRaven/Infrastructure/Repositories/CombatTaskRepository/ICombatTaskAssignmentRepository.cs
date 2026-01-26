//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій "поточних (активних) завдань" по особі.
/// 
/// Призначення:
/// - Це read/write "стан" виконання завдання (одна активна задача на людину).
/// - Використовується для швидких перевірок бізнес-правила:
///     "не можна відкрити нове Start, поки попереднє не закрите".
/// - Є джерелом даних для екрану "хто зараз на завданні" (open assignments),
///   а також для місячних/денних вибірок без складних JOIN до документів.
/// 
/// ВАЖЛИВО:
/// - Assignment НЕ створюється напряму з UI.
/// - Assignment створюється/закривається ТІЛЬКИ коли документ плану стає Posted.
/// - DB гарантія: partial unique index (PersonId where EndedAt is null) => 1 активне завдання.
/// </summary>
public interface ICombatTaskAssignmentRepository
{
    /// <summary>
    /// Повертає активне (відкрите) завдання для особи або null, якщо активного немає.
    /// Використовується для перевірок перед Start/End у проведенні документа.
    /// </summary>
    Task<CombatTaskAssignment?> GetOpenAssignmentForPersonAsync(Guid personId, CancellationToken ct = default);

    /// <summary>
    /// Створює нове завдання (Start). Викликається при Posting документа.
    /// Має впасти (DbUpdateException) якщо у людини вже є відкрите.
    /// </summary>
    Task CreateAssignmentAsync(CombatTaskAssignment assignment, CancellationToken ct = default);

    /// <summary>
    /// Закриває існуюче відкрите завдання (End). Викликається при Posting документа.
    /// Дозволяє закривати і відкривати нове в той самий день (EndedAt == StartedAt нового).
    /// </summary>
    Task CloseAssignmentAsync(Guid assignmentId, DateOnly endedAt, Guid endDocumentId, string author, DateTime nowUtc, CancellationToken ct = default);
}