//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IMissionRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.MissionRepository;

/// <summary>
/// Репозиторій точек місій.
/// 
/// Призначення:
/// - Визначає параметри міссії
/// - Може бути в стані відкрити (CreatedAt) або закритим (ClosedAt)
/// </summary>
public interface IMissionRepository
{
    /// <summary>
    /// Повертає всі записи місій
    /// </summary>
    Task<IReadOnlyList<Mission>> GetMissionPointsAsync(CancellationToken ct = default);

    /// <summary>
    /// Додати місію
    /// </summary>
    Task<Guid> AddMissionPoint(string positionArea, string? namePoint, string? typeDrone, string target, MissionMode missionMode, DateTime todayLocal, CancellationToken ct = default);

    /// <summary>
    /// Закриває місію, шляхом запису поля CloseAt
    /// </summary>
    Task CloseMissionPointAsync(Guid id, DateOnly closeAt, CancellationToken ct = default);
}
