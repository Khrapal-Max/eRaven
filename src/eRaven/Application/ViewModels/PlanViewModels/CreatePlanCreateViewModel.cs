//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanCreateViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public class CreatePlanCreateViewModel
{
    public string PlanNumber { get; set; } = default!;
    public PlanType Type { get; set; }
    public DateTime PlannedAt { get; set; }                    // може бути Unspecified/Local — нормалізуємо в UTC
    public string? Location { get; set; }
    public string? GroupName { get; set; }
    public string? ToolType { get; set; }
    public string? TaskDescription { get; set; }
    public string? Author { get; set; }

    /// <summary>Склад плану — ідентифікатори осіб.</summary>
    public IReadOnlyCollection<Guid> ParticipantIds { get; set; } = [];
}
