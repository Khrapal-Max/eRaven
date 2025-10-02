//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MilitaryDetails
//-----------------------------------------------------------------------------

namespace eRaven.Domain.ValueObjects;

/// <summary>
/// Інформація про військовий стан
/// </summary>
public sealed record MilitaryDetails
{
    /// <summary>
    /// Звання
    /// </summary>
    public string Rank { get; init; } = string.Empty;

    /// <summary>
    /// Наявність військового навчання
    /// </summary>
    public string BZVP { get; init; } = string.Empty;

    /// <summary>
    /// Тип та номер зброї
    /// </summary>
    public string? Weapon { get; init; }

    /// <summary>
    /// Позивний
    /// </summary>
    public string? Callsign { get; init; }

    public MilitaryDetails(string rank, string bzvp, string? weapon = null, string? callsign = null)
    {
        if (string.IsNullOrWhiteSpace(rank))
            throw new ArgumentException("Звання обов'язкове", nameof(rank));

        if (string.IsNullOrWhiteSpace(bzvp))
            throw new ArgumentException("БЗВП обов'язковий", nameof(bzvp));

        Rank = rank.Trim();
        BZVP = bzvp.Trim();
        Weapon = string.IsNullOrWhiteSpace(weapon) ? null : weapon.Trim();
        Callsign = string.IsNullOrWhiteSpace(callsign) ? null : callsign.Trim();
    }
}
