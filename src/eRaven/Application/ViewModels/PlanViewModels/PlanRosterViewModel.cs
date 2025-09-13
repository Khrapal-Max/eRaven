// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanRosterViewModel (кандидати для модала додавання елемента плану)
// -----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

/// <summary>
/// Рядок «роустера» людей для добору в елемент плану.
/// Містить базові відомості, поточний статус і (за потреби) останню планову дію
/// в межах поточного плану з контекстом останнього відрядження.
/// </summary>
public class PlanRosterViewModel
{
    public Guid PersonId { get; init; }
    public string FullName { get; init; } = default!;
    public string? Rnokpp { get; init; }
    public string? Rank { get; init; }
    public string? Position { get; init; }

    // Статус на поточний момент (достатньо для фільтра Dispatch == "В районі"/code=30)
    public string? StatusKindCode { get; init; }
    public string? StatusKindName { get; init; }
}
