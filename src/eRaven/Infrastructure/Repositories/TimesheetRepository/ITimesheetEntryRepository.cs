//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій CRUD для <see cref="TimesheetEntry"/>.
///
/// Призначення:
/// - читання записів табеля (по Id, по особі, по діапазону)
/// - базові операції зміни (Add/Update)
/// - soft-delete (без фізичного видалення)
/// - "перехід" (атомарно: оновити попередній + додати наступний)
///
/// Примітки щодо моделі:
/// - Записи вважаються актуальними, якщо <see cref="TimesheetEntry.IsDeleted"/> == false.
/// - Діапазон запису інклюзивний: [From..To]. Якщо To == null — запис відкритий у майбутнє.
/// </summary>
public interface ITimesheetEntryRepository
{
    //======================================================================
    // Reads
    //======================================================================

    /// <summary>
    /// Повертає запис табеля за його унікальним ідентифікатором.
    /// Soft-deleted записи (IsDeleted=true) ігноруються і повертають null.
    /// </summary>
    Task<TimesheetEntry?> GetByIdAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Повертає всі записи табеля вказаної особи, що перетинаються з діапазоном [from..to] (інклюзивно).
    /// Soft-deleted записи (IsDeleted=true) ігноруються.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає записи табеля для багатьох осіб, що перетинаються з діапазоном [from..to] (інклюзивно).
    /// Soft-deleted записи (IsDeleted=true) ігноруються.
    /// </summary>
    Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(
        IReadOnlyCollection<Guid> personIds,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає активний (той, що покриває дату) запис табеля для вказаної особи на <paramref name="date"/>.
    /// Якщо існує кілька перекривань — повертається "найсвіжіший" за найбільшим From,
    /// далі tie-breaker за Id.
    /// Soft-deleted записи (IsDeleted=true) ігноруються.
    /// </summary>
    Task<TimesheetEntry?> GetActiveEntryOnDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    //======================================================================
    // Writes (CRUD)
    //======================================================================

    /// <summary>
    /// Додає новий запис табеля та зберігає зміни.
    /// </summary>
    Task AddAsync(TimesheetEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Оновлює існуючий запис табеля та зберігає зміни.
    /// </summary>
    Task UpdateAsync(TimesheetEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Soft-delete запису:
    /// - позначає <see cref="TimesheetEntry.IsDeleted"/> = true
    /// - записує audit-поля (DeletedBy/DeletedAtUtc/DeleteReason)
    ///
    /// Якщо запис не знайдений або вже видалений — метод повертається без помилки.
    /// </summary>
    Task SoftDeleteAsync(
        Guid entryId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Атомарна операція "перехід":
    /// - оновити попередній запис (наприклад, закрити To / виставити Updated*)
    /// - додати новий запис
    ///
    /// Виконується в транзакції.
    /// </summary>
    Task SaveTransitionAsync(
        TimesheetEntry prevUpdated,
        TimesheetEntry nextAdded,
        CancellationToken ct = default);
}
