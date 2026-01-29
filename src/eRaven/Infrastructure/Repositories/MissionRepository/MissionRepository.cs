//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.MissionRepository;

public class MissionRepository(
    IDbContextFactory<AppDbContext> dbFactory) : IMissionRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<Mission>> GetMissionsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Missions
            .AsNoTracking()
            .ToArrayAsync(ct);
    }

    public async Task<Guid> AddMission(string positionArea, string? namePoint, string? typeDrone, string target, MissionMode missionMode, DateTime todayLocal, CancellationToken ct = default)
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
            NamePoint = string.IsNullOrWhiteSpace(namePoint) ? "" : namePoint.Trim(),
            TypeDrone = string.IsNullOrWhiteSpace(typeDrone) ? null : typeDrone.Trim(),
            Target = target.Trim(),
            MissionMode = missionMode,
            CreatedAt = DateOnly.FromDateTime(todayLocal),
        };

        await db.Missions.AddAsync(mission, ct);
        await db.SaveChangesAsync(ct);

        return mission.Id;
    }

    public async Task CloseMissionAsync(Guid id, DateOnly closeAt, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var mission = await db.Missions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Міссія не знайдена.");

        if (mission.ClosedAt is not null)
            return; // або throw new InvalidOperationException("Місія вже закрита.");

        if (closeAt < mission.CreatedAt)
            throw new InvalidOperationException("Дата закриття не може бути раніше дати створення.");

        mission.ClosedAt = closeAt;

        await db.SaveChangesAsync(ct);
    }
}