//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnit
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Посада
/// </summary>
public class PositionUnit
{
    public Guid Id { get; set; }

    /// <summary>
    /// Порядковий номер посади
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Код посади
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Коротка назва посади
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Шлях посади
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// номер спеціальності
    /// </summary>
    public string SpecialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Стан посади - активний дійсний
    /// </summary>
    public bool IsActived { get; set; }
}
