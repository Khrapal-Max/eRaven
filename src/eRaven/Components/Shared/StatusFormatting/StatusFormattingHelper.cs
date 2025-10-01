using System;
using System.Collections.Generic;
using System.Linq;
using eRaven.Domain.Models;

namespace eRaven.Components.Shared.StatusFormatting;

/// <summary>
/// Provides shared helpers for rendering status codes consistently across UI components.
/// </summary>
internal static class StatusFormattingHelper
{
    private static readonly Dictionary<string, string> BadgeByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["100"] = "badge rounded-pill text-bg-primary",   // синій
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

    public static string GetBadgeClass(string? code, string? notPresentCode)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("30", StringComparison.OrdinalIgnoreCase))
            return "d-inline-block px-1 small"; // «30» — без підсвітки

        if (!string.IsNullOrWhiteSpace(notPresentCode)
            && code.Equals(notPresentCode, StringComparison.OrdinalIgnoreCase))
        {
            return "badge rounded-pill text-bg-info px-2 py-1";
        }

        return BadgeByCode.TryGetValue(code.Trim(), out var cls)
            ? $"{cls} px-2 py-1"
            : "badge rounded-pill text-bg-primary px-2 py-1";
    }

    public static string GetStatusTitle(
        string? code,
        IEnumerable<StatusKind>? kinds,
        string? notPresentCode = null,
        string? notPresentTitle = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        string? name = null;

        if (kinds is not null)
        {
            name = kinds
                .FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase))?
                .Name;
        }

        if (string.IsNullOrWhiteSpace(name)
            && !string.IsNullOrWhiteSpace(notPresentCode)
            && code.Equals(notPresentCode, StringComparison.OrdinalIgnoreCase))
        {
            name = notPresentTitle;
        }

        return string.IsNullOrWhiteSpace(name) ? code : $"{code} — {name}";
    }
}
