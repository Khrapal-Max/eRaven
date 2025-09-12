//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanContracts (DTO для точкових операцій і даних модала)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

// Запит на додання ОДНОГО елемента з декількома людьми
public sealed class AddElementsRequest
{
    public Guid PlanId { get; init; }
    public PlanType Type { get; init; }
    public DateTime EventAtUtc { get; init; }          // нормалізуйте до UTC 00/15/30/45
    public string? Location { get; init; }
    public string? GroupName { get; init; }
    public string? ToolType { get; init; }
    public string? Note { get; init; }
    public IReadOnlyCollection<Guid> PersonIds { get; init; } = [];
}
