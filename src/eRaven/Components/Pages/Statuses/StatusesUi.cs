//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionsUi (pure helpers, без DI)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Components.Pages.Statuses;

public static class StatusesUi
{
    /// <summary>Пошук підрядка, ігнорує регістр; null/порожнє не матчиться.</summary>
    public static bool Has(string? haystack, string needle) =>
        !string.IsNullOrWhiteSpace(haystack) &&
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    /// <summary>Фільтрація Person за ПІБ/РНОКПП.</summary>
    public static IReadOnlyList<Person> FilterPersons(IEnumerable<Person> source, string? search)
    {
        var s = (search ?? string.Empty).Trim();
        if (s.Length == 0) return [];

        return [.. source.Where(p =>
                Has(p.FirstName, s) ||
                Has(p.LastName, s) ||
                Has(p.MiddleName, s) ||
                Has(p.Rnokpp, s))];
    }

    /// <summary>
    /// Локальний календарний день (Kind=Unspecified/Local) → 00:00 Local → UTC.
    /// Якщо час вже заданий — береться .Date.
    /// </summary>
    public static DateTime ToUtcFromLocalMidnight(DateTime localDateUnspecified)
    {
        var localMidnight = DateTime.SpecifyKind(localDateUnspecified.Date, DateTimeKind.Local);
        return localMidnight.ToUniversalTime();
    }

    /// <summary>Узагальнена мітка статусу для селектів/відображення.</summary>
    public static string StatusLabel(StatusKind s) =>
        string.IsNullOrWhiteSpace(s.Name) ? $"ID {s.Id}" : s.Name.Trim();
}
