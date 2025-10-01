//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PersonStatusService;

public sealed class PersonStatusService(IDbContextFactory<AppDbContext> dbf) : IPersonStatusService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    public async Task<IEnumerable<PersonStatus>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.PersonStatuses.AsNoTracking()
            .Include(p => p.Person)
            .Include(s => s.StatusKind)
            .OrderByDescending(s => s.OpenDate)
            .ThenByDescending(s => s.Sequence)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PersonStatus>> GetHistoryAsync(Guid personId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var list = await StatusPriorityComparer
            .OrderForHistory(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.PersonId == personId && s.IsActive))
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    /// <summary>
    /// «Поточний» статус = останній валідний (IsActive=TRUE) за (OpenDate DESC, Sequence DESC).
    /// </summary>
    public async Task<PersonStatus?> GetActiveAsync(Guid personId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.PersonStatuses.AsNoTracking()
            .Include(s => s.StatusKind)
            .Where(s => s.PersonId == personId && s.IsActive)
            .OrderByDescending(s => s.OpenDate)
            .ThenByDescending(s => s.Sequence)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Встановити новий статус: перевіряємо перехід згідно правил, нормалізуємо момент (UTC),
    /// автоматично підбираємо Sequence (0..n) на ту саму дату/момент, виставляємо Person.StatusKindId.
    /// </summary>
    public async Task<PersonStatus> SetStatusAsync(PersonStatus ps, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        // ====== 1) Валідації та нормалізація ======
        ArgumentNullException.ThrowIfNull(ps);
        if (ps.PersonId == Guid.Empty) throw new ArgumentException("PersonId обовʼязковий.", nameof(ps));
        if (ps.StatusKindId <= 0) throw new ArgumentException("StatusKindId обовʼязковий.", nameof(ps));

        // Нормалізуємо OpenDate до UTC (на вхід може прийти Local/Unspecified)
        var openUtc = ps.OpenDate.Kind switch
        {
            DateTimeKind.Utc => ps.OpenDate,
            DateTimeKind.Local => ps.OpenDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(ps.OpenDate, DateTimeKind.Utc)
        };

        // Перевіряємо існування Person/StatusKind
        var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == ps.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var toKindExists = await db.StatusKinds.AnyAsync(k => k.Id == ps.StatusKindId, ct);
        if (!toKindExists) throw new InvalidOperationException("Вказаний статус не існує.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // ====== 2) Правила переходів: від поточного (останнього валідного) → до нового
        var current = await db.PersonStatuses
            .Where(s => s.PersonId == ps.PersonId && s.IsActive)
            .OrderByDescending(s => s.OpenDate).ThenByDescending(s => s.Sequence)
            .FirstOrDefaultAsync(ct);

        int? fromKindId = current?.StatusKindId;
        if (!await IsTransitionAllowedAsync(fromKindId, ps.StatusKindId, ct))
            throw new InvalidOperationException("Перехід у вказаний статус заборонено правилами.");

        // Забороняємо «назад у часі» відносно поточного
        if (current is not null && openUtc < current.OpenDate)
            throw new InvalidOperationException("Момент має бути пізніший за останній відкритий статус.");

        // ====== 3) Присвоюємо Sequence на цей самий момент часу
        short nextSeq = (await db.PersonStatuses
            .Where(s => s.PersonId == ps.PersonId && s.IsActive && s.OpenDate == openUtc)
            .MaxAsync(s => (short?)s.Sequence, ct)) ?? -1;

        nextSeq++;

        // ====== 4) Створюємо новий «валідний» запис
        var toSave = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = ps.PersonId,
            StatusKindId = ps.StatusKindId,
            OpenDate = openUtc,
            Sequence = nextSeq,
            IsActive = true,
            Note = string.IsNullOrWhiteSpace(ps.Note) ? null : ps.Note.Trim(),
            Author = string.IsNullOrWhiteSpace(ps.Author) ? "system" : ps.Author!.Trim(),
            Modified = DateTime.UtcNow
        };

        db.PersonStatuses.Add(toSave);

        // Оновлюємо «поточний» статус у Person
        person.StatusKindId = ps.StatusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return toSave;
    }

    public async Task<bool> IsTransitionAllowedAsync(int? fromStatusKindId, int toStatusKindId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        // Перша установка — дозволяємо
        if (fromStatusKindId is null) return true;

        return await db.StatusTransitions
            .AnyAsync(t => t.FromStatusKindId == fromStatusKindId && t.ToStatusKindId == toStatusKindId, ct);
    }

    /// <summary>
    /// Перемикає IsActive; при активації уникає конфлікту унікального індексу
    /// (якщо вже є активний з тим самим (person, open, sequence) — піднімаємо Sequence до наступного).
    /// Після зміни перевираховує Person.StatusKindId = останній валідний запис.
    /// </summary>
    public async Task<bool> UpdateStateIsActive(Guid statusId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (statusId == Guid.Empty) throw new ArgumentException("statusId is required.", nameof(statusId));

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var status = await db.PersonStatuses.FirstOrDefaultAsync(s => s.Id == statusId, ct)
            ?? throw new InvalidOperationException("Status not found.");

        var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == status.PersonId, ct)
            ?? throw new InvalidOperationException("Person not found.");

        var turnOn = !status.IsActive;

        if (turnOn)
        {
            // при активації — уникаємо конфлікту унікального індексу (person_id, open_date, sequence) WHERE is_active=TRUE
            var existsActiveSameKey = await db.PersonStatuses.AnyAsync(
                s => s.PersonId == status.PersonId
                  && s.IsActive
                  && s.OpenDate == status.OpenDate
                  && s.Sequence == status.Sequence, ct);

            if (existsActiveSameKey)
            {
                // переносимо на наступний sequence на той самий момент
                short nextSeq = (await db.PersonStatuses
                    .Where(s => s.PersonId == status.PersonId && s.IsActive && s.OpenDate == status.OpenDate)
                    .MaxAsync(s => (short?)s.Sequence, ct)) ?? -1;

                status.Sequence = (short)(nextSeq + 1);
            }
        }

        status.IsActive = turnOn;
        status.Modified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // оновлюємо Person.StatusKindId до останнього валідного
        var latestValid = await db.PersonStatuses
            .Where(s => s.PersonId == status.PersonId && s.IsActive)
            .OrderByDescending(s => s.OpenDate)
            .ThenByDescending(s => s.Sequence)
            .FirstOrDefaultAsync(ct);

        person.StatusKindId = latestValid?.StatusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return status.IsActive;
    }
}
