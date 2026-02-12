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
    /// Повертає наступний запис табеля (за датою From)
    /// після вказаної дати для конкретної особи в межах таймлайну.
    /// </summary>
    Task<TimesheetEntry?> GetNextEntryAfterDateAsync(
        Guid timelineId,
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

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
    /// Повертає активний (той, що покриває дату) запис табеля в межах конкретного таймлайну.
    /// Soft-deleted записи (IsDeleted=true) ігноруються.
    /// </summary>
    /// <remarks>
    /// Важливо для write-path: будь-яка зміна/перехід має працювати тільки в межах таймлайну,
    /// який покриває дату, і ніколи не має "виходити" за межі OpenedAt/ClosedAt.
    /// </remarks>
    Task<TimesheetEntry?> GetActiveEntryOnDateAsync(
        Guid timelineId,
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
    ///  /// Атомарна операція "перехід":
    /// - оновити попередній запис (закрити To / Updated*)
    /// - додати новий запис
    /// Інваріанти:
    /// 1. Вставки/переходи заборонені, якщо таймлайн закритий (ClosedAt != null).
    /// 2. prev/next мають належати одному таймлайну і не виходити за його межі:
    ///    From >= OpenedAt, а якщо ClosedAt != null то To <= ClosedAt і To не може бути null.
    /// </summary>
    Task SaveTransitionAsync(
        TimesheetEntry prevUpdated,
        TimesheetEntry nextAdded,
        CancellationToken ct = default);
}
