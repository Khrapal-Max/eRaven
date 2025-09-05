//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OrderTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models.Helpers;

internal static class OrderTestsHelpers
{
    /// <summary>
    /// Створює план.
    /// Використовується в юніт-тестах.
    /// </summary>
    internal static Plan MakePlan(
        string planNumber = "PL-0001",
        PlanType type = PlanType.Dispatch,
        DateTime? plannedAtUtc = null,
        PlanTimeKind timeKind = PlanTimeKind.Start,
        string? location = "Місто Х",
        string? groupName = "Група А",
        string? toolType = "Екіпаж"
    )
    {
        return new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = planNumber,
            Type = type,
            PlannedAtUtc = plannedAtUtc ?? new DateTime(2025, 01, 01, 17, 30, 0, DateTimeKind.Utc),
            TimeKind = timeKind,
            Location = location,
            GroupName = groupName,
            ToolType = toolType,
            Author = "tester",
            RecordedUtc = new DateTime(2025, 01, 01, 17, 00, 0, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Створює наказ на підставі плану (снапшотить дату з плану).
    /// Використовується в юніт-тестах.
    /// </summary>
    internal static Order CreateFromPlan(Plan plan, string name, string? author, DateTime nowUtc)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name (назва/номер документа) обов'язкова.");

        return new Order
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Plan = plan,
            Name = name.Trim(),
            EffectiveMomentUtc = plan.PlannedAtUtc, // снапшот: не зміниться, якщо план змінять пізніше
            Author = author,
            RecordedUtc = nowUtc
        };
    }
}