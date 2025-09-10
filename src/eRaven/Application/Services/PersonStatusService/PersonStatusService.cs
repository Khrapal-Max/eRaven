//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PersonStatusService;

public sealed class PersonStatusService(AppDbContext db) : IPersonStatusService
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<PersonStatus>> GetAllAsync(CancellationToken ct = default)
        => await _db.PersonStatuses.AsNoTracking()
            .Include(p => p.Person)
            .Include(s => s.StatusKind)
            .OrderByDescending(s => s.OpenDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PersonStatus>> GetHistoryAsync(Guid personId, CancellationToken ct = default)
    {
        var list = await _db.PersonStatuses.AsNoTracking()
            .Include(s => s.StatusKind)
            .Where(s => s.PersonId == personId)
            .OrderByDescending(s => s.OpenDate)
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    public async Task<PersonStatus?> GetActiveAsync(Guid personId, CancellationToken ct = default)
       => await _db.PersonStatuses.AsNoTracking()
           .Include(s => s.StatusKind)
           .Where(s => s.PersonId == personId && s.CloseDate == null && s.IsActive == true)
           .OrderByDescending(s => s.OpenDate)
           .FirstOrDefaultAsync(ct);

    public async Task<PersonStatus> SetStatusAsync(PersonStatus ps, CancellationToken ct = default)
    {
        // ====== 1) Валідації та нормалізація ======
        ArgumentNullException.ThrowIfNull(ps);
        if (ps.PersonId == Guid.Empty) throw new ArgumentException("PersonId обовʼязковий.", nameof(ps));
        if (ps.StatusKindId <= 0) throw new ArgumentException("StatusKindId обовʼязковий.", nameof(ps));
        if (ps.CloseDate is not null) throw new ArgumentException("CloseDate має бути null при встановленні статусу.", nameof(ps));

        // Нормалізуємо OpenDate до UTC (на вхід може прийти Local/Unspecified)
        ps.OpenDate = ps.OpenDate.Kind switch
        {
            DateTimeKind.Utc => ps.OpenDate,
            DateTimeKind.Local => ps.OpenDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(ps.OpenDate, DateTimeKind.Utc)
        };

        // Перевіряємо існування Person/StatusKind
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == ps.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var toKindExists = await _db.StatusKinds.AnyAsync(k => k.Id == ps.StatusKindId, ct);
        if (!toKindExists) throw new InvalidOperationException("Вказаний статус не існує.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // ====== 2) Поточний активний + правила переходу ======
        var active = await _db.PersonStatuses
            .Where(s => s.PersonId == ps.PersonId && s.CloseDate == null && s.IsActive == true)
            .OrderByDescending(s => s.OpenDate)
            .FirstOrDefaultAsync(ct);

        int? fromKindId = active?.StatusKindId;
        if (!await IsTransitionAllowedAsync(fromKindId, ps.StatusKindId, ct))
            throw new InvalidOperationException("Перехід у вказаний статус заборонено правилами.");

        // ====== 3) Перевірка перетинів ======
        var insideClosed = await _db.PersonStatuses
            .Where(s => s.PersonId == ps.PersonId && s.CloseDate != null)
            .AnyAsync(s => ps.OpenDate >= s.OpenDate && ps.OpenDate <= s.CloseDate, ct);

        if (insideClosed)
            throw new InvalidOperationException("Момент потрапляє в існуючий інтервал статусу (перетин).");

        if (active is not null && ps.OpenDate <= active.OpenDate)
            throw new InvalidOperationException("Момент має бути пізніший за відкритий активний статус.");

        // ====== 4) Автозакриття попереднього активного (IsActive не чіпаємо — це легітимність запису, а не “активність” інтервалу)
        if (active is not null)
        {
            active.CloseDate = ps.OpenDate;
            active.Modified = DateTime.UtcNow;
        }

        // ====== 5) Створення нового інтервалу ======
        var toSave = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = ps.PersonId,
            StatusKindId = ps.StatusKindId,
            OpenDate = ps.OpenDate,
            CloseDate = null,
            IsActive = true, // ← новий інтервал за замовчуванням легітимний
            Note = string.IsNullOrWhiteSpace(ps.Note) ? null : ps.Note.Trim(),
            Author = string.IsNullOrWhiteSpace(ps.Author) ? "system" : ps.Author!.Trim(),
            Modified = DateTime.UtcNow
        };

        _db.PersonStatuses.Add(toSave);

        // Оновлюємо "поточний" статус у Person: останній відкритий і легітимний
        person.StatusKindId = ps.StatusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return toSave;
    }

    public async Task<bool> IsTransitionAllowedAsync(int? fromStatusKindId, int toStatusKindId, CancellationToken ct = default)
    {
        if (fromStatusKindId is null) return true;
        return await _db.Set<StatusTransition>()
            .AnyAsync(t => t.FromStatusKindId == fromStatusKindId && t.ToStatusKindId == toStatusKindId, ct);
    }

    /// <summary>
    /// Тумбл легітимності запису. Після зміни перераховуємо Person.StatusKindId
    /// як "останній відкритий і легітимний" інтервал, або null, якщо такого немає.
    /// </summary>
    public async Task<bool> UpdateStateIsActive(Guid statusId, CancellationToken ct = default)
    {
        if (statusId == Guid.Empty) throw new ArgumentException("statusId is required.", nameof(statusId));

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var s = await _db.PersonStatuses.FirstOrDefaultAsync(x => x.Id == statusId, ct)
            ?? throw new InvalidOperationException("Status not found.");

        var wasOpen = s.CloseDate is null;
        var newIsActive = !s.IsActive;
        var now = DateTime.UtcNow;

        // === 1) ВИМКНУТИ легітимність у відкритого → спершу зробити не легітимним, потім ре-опенити prev ===
        if (!newIsActive && wasOpen)
        {
            // 1.1 Поточний перестає бути легітимним (і залишається "відкритим", але IsActive=false)
            s.IsActive = false;
            s.Modified = now;
            await _db.SaveChangesAsync(ct); // <— важливо: тепер у БД немає жодного OPEN & IsActive=true

            // 1.2 Ре-опенимо попередній легітимний (якщо такий є)
            var prev = await _db.PersonStatuses
                .Where(x => x.PersonId == s.PersonId && x.IsActive == true && x.OpenDate < s.OpenDate)
                .OrderByDescending(x => x.OpenDate)
                .FirstOrDefaultAsync(ct);

            if (prev is not null)
            {
                prev.CloseDate = null;
                prev.Modified = now;
                await _db.SaveChangesAsync(ct); // <— тепер prev єдиний OPEN & IsActive=true
            }
        }
        // === 2) УВІМКНУТИ легітимність у відкритого → спершу закрити "інший" OPEN, потім активувати s ===
        else if (newIsActive && wasOpen)
        {
            // 2.1 Закриваємо будь-який інший відкритий легітимний інтервал
            var otherOpen = await _db.PersonStatuses
                .Where(x => x.PersonId == s.PersonId && x.Id != s.Id && x.IsActive == true && x.CloseDate == null)
                .OrderByDescending(x => x.OpenDate)
                .FirstOrDefaultAsync(ct);

            if (otherOpen is not null)
            {
                otherOpen.CloseDate = s.OpenDate; // межа на старт нашого інтервалу
                otherOpen.Modified = now;
                await _db.SaveChangesAsync(ct);   // <— важливо: прибрали конфлікт до активації s
            }

            // 2.2 Робимо наш інтервал легітимним
            s.IsActive = true;
            s.Modified = now;
            await _db.SaveChangesAsync(ct);
        }
        // === 3) Тумбл на закритому інтервалі — просто перемикаємо прапорець ===
        else
        {
            s.IsActive = newIsActive;
            s.Modified = now;
            await _db.SaveChangesAsync(ct);
        }

        // === 4) Перерахувати "поточний" статус Person за останнім OPEN & IsActive=true ===
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == s.PersonId, ct)
            ?? throw new InvalidOperationException("Person not found.");

        var current = await _db.PersonStatuses
            .Where(x => x.PersonId == s.PersonId && x.IsActive == true && x.CloseDate == null)
            .OrderByDescending(x => x.OpenDate)
            .FirstOrDefaultAsync(ct);

        person.StatusKindId = current?.StatusKindId;
        person.ModifiedUtc = now;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return s.IsActive;
    }
}
