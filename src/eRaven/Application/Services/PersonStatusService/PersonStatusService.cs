//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusService
//-----------------------------------------------------------------------------

using eRaven.Application.Projector;
using eRaven.Application.ViewModels.PersonStatusViewModels;
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

    public async Task<IReadOnlyList<PersonStatusHistoryItem>> GetHistoryAsync(Guid personId, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var list = await db.PersonStatuses.AsNoTracking()
            .Where(s => s.PersonId == personId)
            .OrderByDescending(s => s.OpenDate)
            .ThenByDescending(s => s.Sequence)
            .Select(s => new PersonStatusHistoryItem(
                s.Id,
                s.StatusKindId,
                s.StatusKind != null ? s.StatusKind.Code : null,
                s.StatusKind != null ? s.StatusKind.Name : null,
                s.OpenDate,
                s.IsActive,
                s.Sequence,
                s.Note,
                s.Author,
                s.SourceDocumentId,
                s.SourceDocumentType))
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

        ArgumentNullException.ThrowIfNull(ps);
        if (ps.PersonId == Guid.Empty) throw new ArgumentException("PersonId обовʼязковий.", nameof(ps));
        if (ps.StatusKindId <= 0) throw new ArgumentException("StatusKindId обовʼязковий.", nameof(ps));

        var note = string.IsNullOrWhiteSpace(ps.Note) ? null : ps.Note.Trim();
        var author = string.IsNullOrWhiteSpace(ps.Author) ? "system" : ps.Author!.Trim();
        var effectiveUtc = ps.OpenDate.Kind switch
        {
            DateTimeKind.Utc => ps.OpenDate,
            DateTimeKind.Local => ps.OpenDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(ps.OpenDate, DateTimeKind.Utc)
        };

        var person = await db.Persons
            .AsNoTracking()
            .Include(p => p.StatusHistory)
            .ThenInclude(s => s.StatusKind)
            .FirstOrDefaultAsync(p => p.Id == ps.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var statusKind = await db.StatusKinds.FirstOrDefaultAsync(k => k.Id == ps.StatusKindId, ct)
            ?? throw new InvalidOperationException("Вказаний статус не існує.");

        var current = person.CurrentStatus;
        if (current is not null && effectiveUtc < current.OpenDate)
            throw new InvalidOperationException("Момент має бути пізніший за останній відкритий статус.");

        var transitions = await db.StatusTransitions
            .AsNoTracking()
            .ToListAsync(ct);

        var created = person.SetStatus(
            statusKind,
            effectiveUtc,
            note,
            author,
            transitions,
            ps.SourceDocumentId,
            ps.SourceDocumentType);

        created.StatusKind = statusKind;
        created.Person = person;

        await PersonAggregateProjector.ProjectAsync(db, person, ct);
        await db.SaveChangesAsync(ct);

        return created;
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

        var person = await db.Persons
            .AsNoTracking()
            .Include(p => p.StatusHistory)
            .ThenInclude(s => s.StatusKind)
            .FirstOrDefaultAsync(p => p.StatusHistory.Any(s => s.Id == statusId), ct)
            ?? throw new InvalidOperationException("Person not found.");

        var snapshot = person.StatusHistory.First(s => s.Id == statusId);
        var turnOn = !snapshot.IsActive;
        var changed = person.SetStatusActiveState(statusId, turnOn, DateTime.UtcNow);

        if (!changed)
        {
            return snapshot.IsActive;
        }

        snapshot.Person = person;

        if (person.StatusKindId is not null)
        {
            var activeKind = person.StatusHistory
                .Where(s => s.IsActive && s.StatusKindId == person.StatusKindId)
                .Select(s => s.StatusKind)
                .FirstOrDefault();

            if (activeKind is not null)
            {
                person.StatusKind = activeKind;
            }
        }

        await PersonAggregateProjector.ProjectAsync(db, person, ct);
        await db.SaveChangesAsync(ct);

        return snapshot.IsActive;
    }
}
