// -----------------------------------------------------------------------------
// Статичний хелпер для відображення бейджів статусів (кольори + тултіп)
// -----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Components.Shared;

internal static class StatusBadgeHelper
{
    private static readonly IReadOnlyDictionary<string, string> BadgeByCode =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["100"] = "badge rounded-pill text-bg-primary",   // синій
            ["нб"] = "badge rounded-pill text-bg-info",       // блакитний
            ["РОЗПОР"] = "badge rounded-pill text-bg-info",   // блакитний
            ["ВДР"] = "badge rounded-pill text-bg-secondary", // сірий
            ["В"] = "badge rounded-pill text-bg-success",     // зелений
            ["Л_Х"] = "badge rounded-pill text-bg-warning",   // жовтий
            ["Л_Б"] = "badge rounded-pill text-bg-warning",   // жовтий
            ["БВ"] = "badge rounded-pill text-bg-danger",     // червоний
            ["П"] = "badge rounded-pill text-bg-danger",
            ["200"] = "badge rounded-pill text-bg-danger",
            ["А"] = "badge rounded-pill text-bg-danger",
            ["СЗЧ"] = "badge rounded-pill text-bg-danger"
        };

    public static string GetBadgeClass(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("30", StringComparison.OrdinalIgnoreCase))
            return "d-inline-block px-1 small"; // «30» — без підсвітки

        return BadgeByCode.TryGetValue(code.Trim(), out var cls)
            ? $"{cls} px-2 py-1"
            : "badge rounded-pill text-bg-primary px-2 py-1";
    }

    public static string GetStatusTitle(IEnumerable<StatusKind> kinds, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;

        var match = (kinds ?? Array.Empty<StatusKind>())
            .FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(match?.Name)
            ? code
            : $"{code} — {match!.Name}";
    }
}
