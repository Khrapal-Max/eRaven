//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IMissionParticipationRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій інтервалів участі в місіях (MissionParticipation).
/// Концепція: тільки Create (start) / Close (end) / Delete (draft cleanup).
/// </summary>
public interface IMissionParticipationRepository
{
    //======================================================================
    // Queries
    //======================================================================

    /// <summary>
    /// Повертає всі участі, що активні на дату (overlap).
    /// Опційно: фільтр по місії та пошук по snapshot (ПІБ/РНОКПП/позивний).
    /// </summary>
    Task<IReadOnlyList<MissionParticipationRowDto>> GetOnDateAsync(
        DateOnly date,
        Guid? missionId,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає всі участі, що перетинають місяць (overlap).
    /// Опційно: фільтр по місії та пошук по snapshot.
    /// </summary>
    Task<IReadOnlyList<MissionParticipationRowDto>> GetOverlappingMonthAsync(
        int year,
        int month,
        Guid? missionId,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає PersonId тих, хто зайнятий (має overlap) на дату.
    /// Використовується для picker-а "вільні для участі".
    /// </summary>
    Task<IReadOnlyList<Guid>> GetBusyPersonIdsOnDateAsync(DateOnly date, CancellationToken ct = default);

    //======================================================================
    // Commands
    //======================================================================

    /// <summary>
    /// Створює групу відкритих участей (To = null) в межах документа.
    /// Повертає GroupId.
    /// </summary>
    Task<Guid> StartGroupAsync(
        Guid documentId,
        string sourceDocNo,
        Guid missionId,
        string missionDisplaySnapshot,
        DateOnly from,
        IReadOnlyCollection<CombatTaskPersonLookupDto> persons,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває відкриті участі в конкретній групі (To = null → To = date)
    /// та пише EndSourceDocNo.
    /// </summary>
    Task EndGroupAsync(
        Guid documentId,
        Guid groupId,
        DateOnly to,
        string endSourceDocNo,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Фізично видаляє усі рядки групи (MissionParticipation) в межах документа.
    /// Якщо група не знайдена — просто 0 змін.
    /// </summary>
    Task DeleteGroupAsync(
        Guid documentId,
        Guid groupId,
        CancellationToken ct = default);
}
