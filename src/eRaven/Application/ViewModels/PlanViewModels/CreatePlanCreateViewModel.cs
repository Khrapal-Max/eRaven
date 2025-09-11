//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class CreatePlanViewModel
{
    /// <summary>Людський номер плану (унікальний). Зберігайте обрізаним.</summary>
    public string PlanNumber { get; set; } = default!;

    public PlanState State { get; set; } = PlanState.Open;

    /// <summary>Склад плану — ідентифікатори осіб.</summary>
    public IReadOnlyCollection<PlanElement> PlanElements { get; set; } = [];
}
