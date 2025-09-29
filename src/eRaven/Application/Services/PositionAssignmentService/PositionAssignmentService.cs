//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.Shared;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PositionAssignmentService;

public class PositionAssignmentService(IDbContextFactory<AppDbContext> dbf) : IPositionAssignmentService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    public async Task<IReadOnlyList<PersonPositionAssignment>> GetHistoryAsync(Guid personId, int limit = 50, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (limit <= 0) limit = 50;

        var list = await db.PersonPositionAssignments.AsNoTracking()
            .Include(x => x.PositionUnit)
            .Where(x => x.PersonId == personId)
            .OrderByDescending(x => x.OpenUtc)
            .Take(limit)
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    public async Task<PersonPositionAssignment?> GetActiveAsync(Guid personId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.PersonPositionAssignments.AsNoTracking()
           .Include(x => x.PositionUnit)
           .FirstOrDefaultAsync(x => x.PersonId == personId && x.CloseUtc == null, ct);
    }

    public async Task<PersonPositionAssignment> AssignAsync(
    Guid personId,
    Guid positionUnitId,
    DateTime openUtc,
    string? note,
    CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var noteValue = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var openUtcNorm = openUtc.Kind switch
        {
            DateTimeKind.Utc => openUtc,
            DateTimeKind.Local => openUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(openUtc, DateTimeKind.Utc)
        };

        var person = await db.Persons
            .AsNoTracking()
            .Include(p => p.PositionAssignments)
            .FirstOrDefaultAsync(p => p.Id == personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var pos = await db.PositionUnits
            .FirstOrDefaultAsync(p => p.Id == positionUnitId, ct)
            ?? throw new InvalidOperationException("Посада не знайдена.");

        if (!pos.IsActived)
            throw new InvalidOperationException("Неможливо призначити на неактивну посаду.");

        var activeForPerson = person.CurrentAssignment;
        if (activeForPerson is not null && activeForPerson.OpenUtc >= openUtcNorm)
            throw new InvalidOperationException("Дата відкриття має бути пізніше за попереднє призначення.");

        var positionBusy = await db.Persons
            .AnyAsync(p => p.PositionUnitId == positionUnitId && p.Id != personId, ct);
        if (positionBusy)
            throw new InvalidOperationException("Посада вже зайнята іншою особою.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var assignment = person.AssignToPosition(pos, openUtcNorm, noteValue, "ui");
        assignment.PositionUnit = pos;
        assignment.Person = person;

        await PersonAggregateProjector.ProjectAsync(db, person, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return assignment;
    }

    public async Task<bool> UnassignAsync(
        Guid personId,
        DateTime closeUtc,
        string? note,
        CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var noteValue = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var closeUtcNorm = closeUtc.Kind switch
        {
            DateTimeKind.Utc => closeUtc,
            DateTimeKind.Local => closeUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(closeUtc, DateTimeKind.Utc)
        };

        var person = await db.Persons
            .AsNoTracking()
            .Include(p => p.PositionAssignments)
            .FirstOrDefaultAsync(p => p.Id == personId, ct);

        if (person is null) return false;

        var active = person.CurrentAssignment;
        if (active is null) return false;
        if (active.OpenUtc >= closeUtcNorm)
            throw new InvalidOperationException("Дата закриття має бути пізніше дати відкриття.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        person.RemoveFromPosition(closeUtcNorm, noteValue, "ui");

        await PersonAggregateProjector.ProjectAsync(db, person, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
    }
}