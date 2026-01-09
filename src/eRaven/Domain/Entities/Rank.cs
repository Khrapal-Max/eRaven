//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Rank
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Звання
/// </summary>
public class Rank
{
    public Guid Id { get; set; }

    /// <summary>
    /// Назва звання
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Пріоритет номер звання
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Стан звання - активний дійсний
    /// </summary>
    public bool IsActived { get; set; }
}
