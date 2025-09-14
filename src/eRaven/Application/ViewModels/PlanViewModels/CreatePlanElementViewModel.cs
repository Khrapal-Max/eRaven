// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanElementViewModel (мінімальний input для створення 1 елемента)
// -----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class CreatePlanElementViewModel
{
    public Guid PersonId { get; set; }
    public PlanType Type { get; set; }
    public DateTime EventAtUtc { get; set; }   // сервіс перевіряє 00/15/30/45 (UTC)

    // Контекст (для Return може бути null — сервіс підтягне з останнього Dispatch у плані)
    public string? Location { get; set; }
    public string? GroupName { get; set; }
    public string? ToolType { get; set; }

    public string? Note { get; set; }
}
