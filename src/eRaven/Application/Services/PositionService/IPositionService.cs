//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Services.PositionService;

public interface IPositionService
{
    /// <summary>
    /// Повернути всі посади, onlyActive = true
    /// </summary>
    /// <param name="onlyActive"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PositionUnit(<see cref="PositionUnit"/>)</returns>
    Task<IReadOnlyList<PositionUnit>> GetPositionsAsync(bool onlyActive = true, CancellationToken ct = default);

    /// <summary>
    /// Повернути посаду по ид
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>PositionUnit?(<see cref="PositionUnit"/>)</returns>
    Task<PositionUnit?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Додати посаду
    /// </summary>
    /// <param name="positionUnit"></param>
    /// <param name="ct"></param>
    /// <returns>PositionUnit(<see cref="PositionUnit"/>)</returns>
    Task<PositionUnit> CreatePositionAsync(PositionUnit positionUnit, CancellationToken ct = default);

    /// <summary>
    /// Деактивувати/активувати посаду
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> SetActiveStateAsync(Guid id, bool isActive, CancellationToken ct = default);
    /// <summary>
    /// Перевірка коду серед активних посад
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> CodeExistsActiveAsync(string code, CancellationToken ct = default);
}
