//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IStatusKindService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.StatusKindService;

public interface IStatusKindService
{
    /// <summary>
    /// Повертає всі статуси
    /// </summary>
    /// <param name="includeInactive"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList StatusKind(<see cref="StatusKind"/>)</returns>
    Task<IReadOnlyList<StatusKind>> GetAllAsync(bool includeInactive = true, CancellationToken ct = default);

    /// <summary>
    /// Повертає статус за ідентифікатором
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>StatusKind?(<see cref="StatusKind"/></returns>
    Task<StatusKind?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Створює новий статус
    /// </summary>
    /// <param name="newKindViewModel"></param>
    /// <param name="ct"></param>
    /// <returns>StatusKind(<see cref="StatusKind"/></returns>
    Task<StatusKind> CreateAsync(CreateKindViewModel newKindViewModel, CancellationToken ct = default);

    /// <summary>
    /// Змінює стан активності статусу
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Змінює порядок статусу
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newOrder"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> UpdateOrderAsync(int id, int newOrder, CancellationToken ct = default); // опційно

    /// <summary>
    /// Перевіряє чи існує назва
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newOrder"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> NameExistsAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Перевіряє чи існує код
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newOrder"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
}
