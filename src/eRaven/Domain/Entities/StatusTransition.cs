//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransition
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Транзакція статуса
/// </summary>
public class StatusTransition
{
    public int Id { get; set; }

    /// <summary>
    /// Поточний статус
    /// </summary>
    public int FromStatusKindId { get; set; }

    /// <summary>
    /// Дозволений статус
    /// </summary>
    public int ToStatusKindId { get; set; }
}
