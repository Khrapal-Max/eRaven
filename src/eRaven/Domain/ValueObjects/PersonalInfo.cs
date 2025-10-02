//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonalInfo
//-----------------------------------------------------------------------------

namespace eRaven.Domain.ValueObjects;

/// <summary>
/// Інформація про людину
/// </summary>
public sealed record PersonalInfo
{
    /// <summary>
    /// ІПН (Ідентифікаційний податковий номер) має бути 10 цифр
    /// </summary>
    public string Rnokpp { get; init; } = string.Empty;

    /// <summary>
    /// Прізвище
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Ім'я
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// По батькові
    /// </summary>
    public string? MiddleName { get; init; }

    // Валідація в конструкторі
    public PersonalInfo(string rnokpp, string lastName, string firstName, string? middleName = null)
    {
        if (string.IsNullOrWhiteSpace(rnokpp))
            throw new ArgumentException("РНОКПП обов'язковий", nameof(rnokpp));

        if (rnokpp.Length != 10 || !rnokpp.All(char.IsDigit))
            throw new ArgumentException("РНОКПП має містити рівно 10 цифр", nameof(rnokpp));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Прізвище обов'язкове", nameof(lastName));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Ім'я обов'язкове", nameof(firstName));

        Rnokpp = rnokpp.Trim();
        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
    }

    public string FullName => string.Join(" ",
        new[] { LastName, FirstName, MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
