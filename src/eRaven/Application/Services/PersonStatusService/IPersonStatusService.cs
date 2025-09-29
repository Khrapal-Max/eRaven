//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonStatusService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonStatusViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PersonStatusService;

public interface IPersonStatusService
{
    /// <summary>
    /// Повертає всі статуси (для довідника).
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>IEnumerable PersonStatus(<see cref="PersonStatus"/>)</returns>
    Task<IEnumerable<PersonStatus>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Вся історія для особи (найновіші — першими).
    /// </summary>
    /// <returns>IReadOnlyList PersonStatusHistoryItem(<see cref="PersonStatusHistoryItem"/>)</returns>
    Task<IReadOnlyList<PersonStatusHistoryItem>> GetHistoryAsync(Guid personId, CancellationToken ct = default);

    /// <summary>
    /// Поточний (незакритий) статус для особи або null.
    /// </summary>
    /// <returns>PersonStatus?(<see cref="PersonStatus"/>)</returns>
    Task<PersonStatus?> GetActiveAsync(Guid personId, CancellationToken ct = default);

    /// <summary>
    /// Встановити статус (створити новий інтервал).
    /// Очікуємо: personStatus.Id порожній (Guid.Empty), CloseDate == null, OpenDate — момент в UTC.
    /// Повертає збережений рядок з присвоєним Id.
    /// </summary>
    /// <returns>PersonStatus(<see cref="PersonStatus"/>)</returns>
    Task<PersonStatus> SetStatusAsync(PersonStatus personStatus, CancellationToken ct = default);

    /// <summary>
    /// Чи дозволений перехід з from→to згідно довідника переходів.
    /// </summary>
    /// <returns>bool</returns>
    Task<bool> IsTransitionAllowedAsync(int? fromStatusKindId, int toStatusKindId, CancellationToken ct = default);

    /// <summary>
    /// Зміна стану IsActive на протилежний
    /// </summary>
    /// <param name="statusId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> UpdateStateIsActive(Guid statusId, CancellationToken ct = default);
}
