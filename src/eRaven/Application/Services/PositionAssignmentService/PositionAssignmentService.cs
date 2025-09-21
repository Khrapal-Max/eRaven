//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PositionAssignmentService;

public class PositionAssignmentService(AppDbContext db) : IPositionAssignmentService
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<PersonPositionAssignment>> GetHistoryAsync(Guid personId, int limit = 50, CancellationToken ct = default)
    {
        if (limit <= 0) limit = 50;

        var list = await _db.PersonPositionAssignments.AsNoTracking()
            .Include(x => x.PositionUnit)
            .Where(x => x.PersonId == personId)
            .OrderByDescending(x => x.OpenUtc)
            .Take(limit)
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    public Task<PersonPositionAssignment?> GetActiveAsync(Guid personId, CancellationToken ct = default) =>
        _db.PersonPositionAssignments.AsNoTracking()
           .Include(x => x.PositionUnit)
           .FirstOrDefaultAsync(x => x.PersonId == personId && x.CloseUtc == null, ct);   

    public async Task<PersonPositionAssignment> AssignAsync(
        Guid personId,
        Guid positionUnitId,
        DateTime openUtc,
        string? note,
        CancellationToken ct = default)
    {
        if (openUtc.Kind != DateTimeKind.Utc)
            openUtc = DateTime.SpecifyKind(openUtc, DateTimeKind.Utc);

        // Перевіряємо існування сутностей
        var person = await _db.Persons.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var pos = await _db.PositionUnits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == positionUnitId, ct)
            ?? throw new InvalidOperationException("Посада не знайдена.");

        if (!pos.IsActived)
            throw new InvalidOperationException("Неможливо призначити на неактивну посаду.");

        // Транзакція: закриваємо активні, призначаємо нову, оновлюємо pointer у Person
        using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) Закриваємо активне призначення людини (якщо є)
        var activeForPerson = await _db.PersonPositionAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);

        if (activeForPerson is not null)
        {
            if (activeForPerson.OpenUtc >= openUtc)
                throw new InvalidOperationException("Дата відкриття має бути пізніше за попереднє призначення.");

            activeForPerson.CloseUtc = openUtc;
            activeForPerson.ModifiedUtc = DateTime.UtcNow;
        }

        // 2) Перевіряємо, що посада не зайнята (індекс теж захистить, але краще явна перевірка)
        var posOccupied = await _db.PersonPositionAssignments
            .AnyAsync(a => a.PositionUnitId == positionUnitId && a.CloseUtc == null, ct);

        if (posOccupied)
            throw new InvalidOperationException("Посада вже зайнята іншою особою.");

        // 3) Створюємо новий запис
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

        await _db.PersonPositionAssignments.AddAsync(assign, ct);

        // 4) Оновлюємо pointer у Person
        person.PositionUnitId = positionUnitId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Повертаємо з навігацією
        assign.PositionUnit = await _db.PositionUnits.AsNoTracking().FirstAsync(x => x.Id == positionUnitId, ct);
        return assign;
    }

    public async Task<bool> UnassignAsync(
        Guid personId,
        DateTime closeUtc,
        string? note,
        CancellationToken ct = default)
    {
        if (closeUtc.Kind != DateTimeKind.Utc)
            closeUtc = DateTime.SpecifyKind(closeUtc, DateTimeKind.Utc);

        var person = await _db.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, ct);
        if (person is null) return false;

        var active = await _db.PersonPositionAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);

        if (active is null) return false; // нема що знімати

        if (active.OpenUtc >= closeUtc)
            throw new InvalidOperationException("Дата закриття має бути пізніше дати відкриття.");

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) Закриваємо запис
        active.CloseUtc = closeUtc;
        active.Note = string.IsNullOrWhiteSpace(note) ? active.Note : note.Trim();
        active.ModifiedUtc = DateTime.UtcNow;

        // 2) Очищаємо pointer у Person
        person.PositionUnitId = null;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
    }
}