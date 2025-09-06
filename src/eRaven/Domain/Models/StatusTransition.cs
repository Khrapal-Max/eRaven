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
    public StatusKind FromStatusKind { get; set; } = null!;

    /// <summary>
    /// Дозволений статус
    /// </summary>
    public int ToStatusKindId { get; set; }
    public StatusKind ToStatusKind { get; set; } = null!;
}
