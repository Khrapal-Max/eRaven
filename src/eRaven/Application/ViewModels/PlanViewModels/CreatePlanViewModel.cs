//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class CreatePlanViewModel
{
    /// <summary>Людський номер плану (унікальний). Зберігайте обрізаним.</summary>
    public string PlanNumber { get; set; } = default!;

    public PlanState State { get; set; } = PlanState.Open;
}
