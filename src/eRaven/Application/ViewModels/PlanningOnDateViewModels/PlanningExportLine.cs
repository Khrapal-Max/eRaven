//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanningExportLine
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanningOnDateViewModels;

// ====================== Export line model =======================

public sealed class PlanningExportLine
{
    // “Шапка блоку”: заповнюємо лише ці три
    public string? Location { get; init; }
    public string? GroupName { get; init; }
    public string? CrewName { get; init; }

    // Рядок людини: заповнюємо ці, перші 3 — null
    public string? RankName { get; init; }
    public string? FullName { get; init; }
    public string? Callsign { get; init; }
    public string? PlanActionName { get; init; }
    public string? Order { get; init; }
    public DateTime? EffectiveAtUtc { get; init; } // nullable для “шапок”
    public string? Note { get; init; }
}
