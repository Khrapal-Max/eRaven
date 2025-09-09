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
           .Where(s => s.PersonId == personId && s.CloseDate == null)
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
            .Where(s => s.PersonId == ps.PersonId && s.CloseDate == null)
            .OrderByDescending(s => s.OpenDate)
            .FirstOrDefaultAsync(ct);

        int? fromKindId = active?.StatusKindId;
        if (!await IsTransitionAllowedAsync(fromKindId, ps.StatusKindId, ct))
            throw new InvalidOperationException("Перехід у вказаний статус заборонено правилами.");

        // ====== 3) Перевірка перетинів ======
        // Забороняємо момент всередині будь-якого закритого інтервалу
        var insideClosed = await _db.PersonStatuses
            .Where(s => s.PersonId == ps.PersonId && s.CloseDate != null)
            .AnyAsync(s => ps.OpenDate >= s.OpenDate && ps.OpenDate <= s.CloseDate, ct);

        if (insideClosed)
            throw new InvalidOperationException("Момент потрапляє в існуючий інтервал статусу (перетин).");

        // Якщо є активний — новий момент має бути > за його OpenDate
        if (active is not null && ps.OpenDate <= active.OpenDate)
            throw new InvalidOperationException("Момент має бути пізніший за відкритий активний статус.");

        // ====== 4) Автозакриття попереднього активного ======
        if (active is not null)
        {
            active.CloseDate = ps.OpenDate;
            active.IsActive = false;
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
            IsActive = true,
            Note = string.IsNullOrWhiteSpace(ps.Note) ? null : ps.Note.Trim(),
            Author = string.IsNullOrWhiteSpace(ps.Author) ? "system" : ps.Author!.Trim(),
            Modified = DateTime.UtcNow
        };

        _db.PersonStatuses.Add(toSave);

        // Оновлюємо "поточний" статус у Person (nullable)
        person.StatusKindId = ps.StatusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return toSave;
    }

    public async Task<bool> IsTransitionAllowedAsync(int? fromStatusKindId, int toStatusKindId, CancellationToken ct = default)
    {
        // Перша установка — дозволяємо (якщо хочеш жорсткіше — введи "стартові" правила)
        if (fromStatusKindId is null) return true;

        return await _db.Set<StatusTransition>()
            .AnyAsync(t => t.FromStatusKindId == fromStatusKindId && t.ToStatusKindId == toStatusKindId, ct);
    }
}
