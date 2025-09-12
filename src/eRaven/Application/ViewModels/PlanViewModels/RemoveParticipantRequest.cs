//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanContracts (DTO для точкових операцій і даних модала)
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanViewModels;

// Видалити конкретного учасника зі складу елемента
public sealed record RemoveParticipantRequest(Guid PlanId, Guid ElementId, Guid PersonId);
