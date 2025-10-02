//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionAssignmentPolicy
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;

namespace eRaven.Domain.Services;

// Реалізація
public class PositionAssignmentPolicy : IPositionAssignmentPolicy
{
    private readonly IPositionUnitRepository _positionRepository;
    private readonly IPersonRepository _personRepository;
    public PositionAssignmentPolicy(
        IPositionUnitRepository positionRepository,
        IPersonRepository personRepository)
    {
        _positionRepository = positionRepository;
        _personRepository = personRepository;
    }

    public bool CanAssignToPosition(Guid positionUnitId)
    {
        // 1. Посада має існувати
        var position = _positionRepository.GetById(positionUnitId);
        if (position == null) return false;

        // 2. Посада має бути активною
        if (!position.IsActived) return false;

        // 3. Посада має бути вільною (жоден Person.PositionUnitId != positionUnitId)
        var isOccupied = _personRepository.IsPositionOccupied(positionUnitId);
        return !isOccupied;
    }
}