// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanActionPrefillViewModel
// -----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanViewModels;

/// <summary>
/// Підказки для автозаповнення при виборі особи.
/// </summary>
public sealed record PlanActionPrefillViewModel(
    string? Location,
    string? GroupName,
    string? CrewName,
    DateTime SuggestedEventAtUtc // вже у UTC і на найближчий “квартал”
);
