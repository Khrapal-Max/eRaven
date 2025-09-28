//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// IPositionAssignmentService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Services.PositionAssignmentService;

public interface IPositionAssignmentService
{
    Task<IReadOnlyList<PersonPositionAssignment>> GetHistoryAsync(Guid personId, int limit = 50, CancellationToken ct = default);

    Task<PersonPositionAssignment?> GetActiveAsync(Guid personId, CancellationToken ct = default);

    Task<PersonPositionAssignment> AssignAsync(
        Guid personId,
        Guid positionUnitId,
        DateTime openUtc,
        string? note,
        CancellationToken ct = default);

    Task<bool> UnassignAsync(
        Guid personId,
        DateTime closeUtc,
        string? note,
        CancellationToken ct = default);
}
