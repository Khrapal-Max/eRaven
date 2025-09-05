//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnit
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Посада
/// </summary>
public class PositionUnit
{
    public Guid Id { get; set; }

    /// <summary>
    /// Код посади
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Коротка назва посади
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Шлях посади
    /// </summary>
    public string? OrgPath { get; set; }

    /// <summary>
    /// Навігація до поточного власника (без FK-властивості тут!)
    /// </summary>
    public Person? CurrentPerson { get; set; }

    /// <summary>
    /// Конкатенація повної назви посади
    /// </summary>
    public string FullName =>
       string.IsNullOrWhiteSpace(OrgPath) ? ShortName : $"{ShortName} {OrgPath}";
}
