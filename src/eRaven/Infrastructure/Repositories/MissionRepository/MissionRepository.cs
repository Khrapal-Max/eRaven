//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.MissionRepository;

/// <summary>
/// Репозиторій точок місій.
/// </summary>
public sealed class MissionRepository(
    IDbContextFactory<AppDbContext> dbFactory) : IMissionRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Mission>> GetMissionsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Missions
            .AsNoTracking()
            .ToArrayAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid> AddMission(
        string positionArea,
        string? namePoint,
        string? typeDrone,
        string target,
        MissionMode missionMode,
        DateTime todayLocal,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(positionArea))
            throw new ArgumentException("Позиційний район не вказаний.", nameof(positionArea));

        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Мета місії не вказана.", nameof(target));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = positionArea.Trim(),

            // Domain: nullable => null when missing (not "")
            NamePoint = string.IsNullOrWhiteSpace(namePoint) ? null : namePoint.Trim(),

            TypeDrone = string.IsNullOrWhiteSpace(typeDrone) ? null : typeDrone.Trim(),
            Target = target.Trim(),
            MissionMode = missionMode,
            CreatedAt = DateOnly.FromDateTime(todayLocal),
        };

        await db.Missions.AddAsync(mission, ct);
        await db.SaveChangesAsync(ct);

        return mission.Id;
    }

    /// <inheritdoc />
    public async Task CloseMissionAsync(Guid id, DateOnly closeAt, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var mission = await db.Missions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Міссія не знайдена.");

        if (mission.ClosedAt is not null)
            return;

        if (closeAt < mission.CreatedAt)
            throw new InvalidOperationException("Дата закриття не може бути раніше дати створення.");

        // Strategy: close mission only if there is NO active assignment for this mission at closeAt.
        // Active = From <= closeAt AND (To is null OR closeAt < To).
        /* var hasActiveAssignments = await db.MissionAssignments
             .AsNoTracking()
             .AnyAsync(x =>
                 x.MissionId == id
                 && x.From <= closeAt
                 && (!x.To.HasValue || closeAt < x.To.Value),
                 ct);

         if (hasActiveAssignments)
             throw new InvalidOperationException("Неможливо закрити міссію, оскільки вона має активні призначення.");

         mission.ClosedAt = closeAt;*/

        await db.SaveChangesAsync(ct);
    }
}
