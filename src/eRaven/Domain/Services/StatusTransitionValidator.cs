//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionValidator
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;

namespace eRaven.Domain.Services;

// Реалізація (використовує репозиторій StatusTransition)
public class StatusTransitionValidator : IStatusTransitionValidator
{
    private readonly IStatusTransitionRepository _repository;
    private readonly IStatusKindRepository _statusKindRepository;
    public StatusTransitionValidator(
        IStatusTransitionRepository repository,
        IStatusKindRepository statusKindRepository)
    {
        _repository = repository;
        _statusKindRepository = statusKindRepository;
    }

    public bool IsValidInitialStatus(int statusKindId)
    {
        // Початковим може бути лише "В районі" (код "30")
        var statusKind = _statusKindRepository.GetById(statusKindId);
        return statusKind?.Code == "30";
    }

    public bool IsTransitionAllowed(int? fromStatusKindId, int toStatusKindId)
    {
        // Перший статус (якщо fromStatusKindId == null) — завжди дозволено
        if (fromStatusKindId == null)
            return IsValidInitialStatus(toStatusKindId);

        // Перевіряємо наявність переходу в таблиці StatusTransitions
        return _repository.IsTransitionAllowed(fromStatusKindId.Value, toStatusKindId);
    }
}