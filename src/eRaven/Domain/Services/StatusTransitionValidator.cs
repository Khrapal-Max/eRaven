//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionValidator
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Services;

// Реалізація (використовує репозиторій StatusTransition)
public class StatusTransitionValidator : IStatusTransitionValidator
{
    public bool IsValidInitialStatus(int statusKindId)
    {
        // Початковим може бути лише "Рекрут" (ID = 1 згідно seed)
        return statusKindId == 1;
    }

    public bool IsTransitionAllowed(
        int? fromStatusKindId,
        int toStatusKindId,
        HashSet<int> allowedTransitions)
    {
        // Перший статус - завжди дозволено якщо валідний
        if (fromStatusKindId == null)
            return IsValidInitialStatus(toStatusKindId);

        // Перевіряємо чи є перехід у дозволених
        return allowedTransitions.Contains(toStatusKindId);
    }
}