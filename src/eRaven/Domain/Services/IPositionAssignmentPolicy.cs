//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPositionAssignmentPolicy
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Services;

public interface IPositionAssignmentPolicy
{
    bool CanAssignToPosition(Guid positionUnitId);
}