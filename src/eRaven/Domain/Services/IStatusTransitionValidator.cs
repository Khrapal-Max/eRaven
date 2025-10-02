//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IStatusTransitionValidator
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Services;

// Інтерфейс для валідації переходів статусів
public interface IStatusTransitionValidator
{
    bool IsValidInitialStatus(int statusKindId);
    bool IsTransitionAllowed(int? fromStatusKindId, int toStatusKindId);
}
