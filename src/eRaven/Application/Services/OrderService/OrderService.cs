using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.OrderService;

public class OrderService(AppDbContext db) : IOrderService
{
    private readonly AppDbContext _db = db;

    public async Task<bool> AppointAsync(Guid personId, Guid positionUnitId, DateTime effectiveUtc,
                                         string? note, string? author, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId, ct)
                     ?? throw new InvalidOperationException("Особа не знайдена.");

        // посада не зайнята?
        var posBusy = await _db.PersonPositionAssignments
            .AnyAsync(a => a.PositionUnitId == positionUnitId && a.CloseUtc == null, ct);
        if (posBusy) throw new InvalidOperationException("Посада вже зайнята.");

        // в особи нема активного призначення?
        var active = await _db.PersonPositionAssignments
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);
        if (active != null) throw new InvalidOperationException("В особи вже є активне призначення.");

        // створюємо призначення
        var rec = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = positionUnitId,
            OpenUtc = effectiveUtc,
            Note = note,
            Author = author ?? "system",
            ModifiedUtc = DateTime.UtcNow
        };
        _db.PersonPositionAssignments.Add(rec);

        // оновлюємо поточну посаду у Person (для UI)
        person.PositionUnitId = positionUnitId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TransferAsync(Guid personId, Guid newPositionUnitId, DateTime effectiveUtc,
                                          string? note, string? author, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId, ct)
                     ?? throw new InvalidOperationException("Особа не знайдена.");

        var current = await _db.PersonPositionAssignments
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct)
            ?? throw new InvalidOperationException("Активного призначення не знайдено.");

        if (effectiveUtc <= current.OpenUtc)
            throw new InvalidOperationException("Дата переведення має бути пізніше дати поточного призначення.");

        // закриваємо поточне
        current.CloseUtc = effectiveUtc;
        current.Note = note ?? current.Note;
        current.Author = author ?? current.Author;
        current.ModifiedUtc = DateTime.UtcNow;

        // перевіряємо, що нова посада вільна
        var posBusy = await _db.PersonPositionAssignments
            .AnyAsync(a => a.PositionUnitId == newPositionUnitId && a.CloseUtc == null, ct);
        if (posBusy) throw new InvalidOperationException("Нова посада вже зайнята.");

        // відкриваємо нове призначення з тієї ж дати
        var next = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = newPositionUnitId,
            OpenUtc = effectiveUtc,
            Note = note,
            Author = author ?? "system",
            ModifiedUtc = DateTime.UtcNow
        };
        _db.PersonPositionAssignments.Add(next);

        // оновлюємо Person
        person.PositionUnitId = newPositionUnitId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> DismissAsync(Guid personId, DateTime effectiveUtc,
                                         string? note, string? author, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId, ct)
                     ?? throw new InvalidOperationException("Особа не знайдена.");

        var active = await _db.PersonPositionAssignments
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.CloseUtc == null, ct);

        if (active is null) return false;

        if (effectiveUtc <= active.OpenUtc)
            throw new InvalidOperationException("Дата зняття має бути пізніше дати призначення.");

        active.CloseUtc = effectiveUtc;
        active.Note = note ?? active.Note;
        active.Author = author ?? active.Author;
        active.ModifiedUtc = DateTime.UtcNow;

        person.PositionUnitId = null;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
