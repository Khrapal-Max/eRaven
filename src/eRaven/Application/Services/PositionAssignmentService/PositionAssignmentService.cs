//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentService
//-----------------------------------------------------------------------------

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

        if (openUtc.Kind != DateTimeKind.Utc)
            openUtc = DateTime.SpecifyKind(openUtc, DateTimeKind.Utc);

        // 1) Читаємо ТРЕКАНО
        var person = await db.Persons
            .FirstOrDefaultAsync(p => p.Id == personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var pos = await db.PositionUnits
            .FirstOrDefaultAsync(p => p.Id == positionUnitId, ct)
            ?? throw new InvalidOperationException("Посада не знайдена.");

        if (!pos.IsActived)
            throw new InvalidOperationException("Неможливо призначити на неактивну посаду.");

        using var tx = await db.Database.BeginTransactionAsync(ct);

        // 2) Закриваємо актив для особи (ТРЕКАНО)
        var activeForPerson = await db.PersonPositionAssignments
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);

        if (activeForPerson is not null)
        {
            if (activeForPerson.OpenUtc >= openUtc)
                throw new InvalidOperationException("Дата відкриття має бути пізніше за попереднє призначення.");

            activeForPerson.CloseUtc = openUtc;
            activeForPerson.ModifiedUtc = DateTime.UtcNow;
        }

        // 3) Перевіряємо, що посада вільна (індекс теж захистить)
        var posOccupied = await db.PersonPositionAssignments
            .AnyAsync(a => a.PositionUnitId == positionUnitId && a.CloseUtc == null, ct);
        if (posOccupied)
            throw new InvalidOperationException("Посада вже зайнята іншою особою.");

        // 4) Створюємо новий запис (ТРЕКАНО)
        var assign = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = positionUnitId,
            OpenUtc = openUtc,
            CloseUtc = null,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Author = "ui",
            ModifiedUtc = DateTime.UtcNow
        };
        await db.PersonPositionAssignments.AddAsync(assign, ct);

        // 5) Оновлюємо pointer у Person (це забезпечує правильний CurrentPerson)
        person.PositionUnitId = positionUnitId;
        person.ModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // 6) Повертаємо з навігацією (для UI)
        assign.PositionUnit = await db.PositionUnits
            .AsNoTracking()
            .FirstAsync(x => x.Id == positionUnitId, ct);

        return assign;
    }

    public async Task<bool> UnassignAsync(
        Guid personId,
        DateTime closeUtc,
        string? note,
        CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (closeUtc.Kind != DateTimeKind.Utc)
            closeUtc = DateTime.SpecifyKind(closeUtc, DateTimeKind.Utc);

        // ТРЕКАНО
        var person = await db.Persons
            .FirstOrDefaultAsync(p => p.Id == personId, ct);
        if (person is null) return false;

        var active = await db.PersonPositionAssignments
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);
        if (active is null) return false;

        if (active.OpenUtc >= closeUtc)
            throw new InvalidOperationException("Дата закриття має бути пізніше дати відкриття.");

        using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Закриваємо запис
        active.CloseUtc = closeUtc;
        if (!string.IsNullOrWhiteSpace(note))
            active.Note = note.Trim();
        active.ModifiedUtc = DateTime.UtcNow;

        // 2) Очищаємо pointer у Person
        person.PositionUnitId = null;
        person.ModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
    }
}