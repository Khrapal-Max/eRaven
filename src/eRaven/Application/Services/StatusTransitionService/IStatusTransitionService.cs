//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IStatusTransitionService
//-----------------------------------------------------------------------------

namespace eRaven.Application.Services.StatusTransitionService;

public interface IStatusTransitionService
{
    /// <summary>
    /// Повна карта переходів: FromId -> HashSet(ToId)
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>Dictionary int, HashSet int</returns>
    Task<Dictionary<int, HashSet<int>>> GetAllMapAsync(CancellationToken ct = default);

    /// <summary>
    /// Отримати дозволені To для конкретного From.
    /// </summary>
    /// <param name="fromStatusKindId"></param>
    /// <param name="ct"></param>
    /// <returns>HashSet int</returns>
    Task<HashSet<int>> GetToIdsAsync(int fromStatusKindId, CancellationToken ct = default);

    /// <summary>
    /// Зберегти перелік дозволених To для конкретного From (дифом, у транзакції)
    /// </summary>
    /// <param name="fromStatusKindId"></param>
    /// <param name="allowedToIds"></param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    Task SaveAllowedAsync(int fromStatusKindId, IReadOnlyCollection<int> allowedToIds, CancellationToken ct = default);
}
