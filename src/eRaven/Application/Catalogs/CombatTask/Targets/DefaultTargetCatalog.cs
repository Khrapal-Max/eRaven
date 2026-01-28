//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultTargetCatalog
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace eRaven.Application.Catalogs.CombatTask.Targets;

/// <summary>
/// Каталог цілей бойових завдань
/// - використовується в CombatTask documents (Доукменти планування)
/// </summary>
public sealed class DefaultTargetCatalog : ITargetCatalog
{
    private static readonly IReadOnlyList<TargetOption> _all = new ReadOnlyCollection<TargetOption>(
    [
        new TargetOption("Бойовий екіпаж СтП БпАК з розвідки"),
        new TargetOption("Бойовий екіпаж СтП БпАК з ураження"),
        new TargetOption("Охорона екіпажу СтП БпАК"),
        new TargetOption("Охорона РТЗ"),
        new TargetOption("Інженерні роботи"),
        new TargetOption("Забезпечення екіпажу СтП БпАК(водії)")
    ]);

    public IReadOnlyList<TargetOption> GetActive()
        => [.. _all];
}