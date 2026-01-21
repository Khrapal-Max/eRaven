//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public sealed class TimesheetRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // повертає місячні табелі для всіх осіб, які були в табелі в цей місяць
    public async Task<IReadOnlyList<TimesheetMonthPerPersonDto>> GetMonthlyTimesheetAsync(
     int year, int month, string? search, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var personsQ = db.PersonRead
            .AsNoTracking()
            .Where(p =>
                p.EnrolledAt != null &&
                p.EnrolledAt <= monthEnd &&
                (p.ExcludedAt == null || p.ExcludedAt >= monthStart));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();

            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.EnrollmentKind,
                p.PositionSort,
                p.Position,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return [];

        var personIds = persons.Select(x => x.Id).ToList();

        // 1) entries that overlap month
        var entries = await db.TimesheetEntries
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Where(e => personIds.Contains(e.PersonId))
            .Where(e => e.From <= monthEnd && (e.To == null || e.To >= monthStart))
            .ToListAsync(ct);

        var entriesByPerson = entries
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2) existing monthly read models (include Days)
        var rms = await db.Set<MonthlyTimesheetReadModel>()
            .AsNoTracking()
            .Include(x => x.Days)
            .Where(x => x.Year == year && x.Month == month)
            .ToDictionaryAsync(x => x.PersonId, ct);

        // 3) build result
        var result = new List<TimesheetMonthPerPersonDto>(persons.Count);

        foreach (var p in persons)
        {
            rms.TryGetValue(p.Id, out var rm);

            var baseDays = BuildDaysForMonth(
                year, month,
                monthStart: monthStart,
                monthEnd: monthEnd,
                entries: entriesByPerson.TryGetValue(p.Id, out var list) ? list : null);

            var days = baseDays.AsReadOnly();

            result.Add(new TimesheetMonthPerPersonDto(
                PersonId: p.Id,
                FullName: p.FullName,
                RNOKPP: p.Rnokpp,
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                Timesheet: new MonthlyTimesheetReadModelDto(
                    PersonId: p.Id,
                    Year: year,
                    Month: month,
                    Days: days,
                    UpdatedAtUtc: rm?.UpdatedAtUtc ?? DateTime.MinValue
                )));
        }

        return result;
    }

    // “Стан на дату” для всіх осіб, які були в табелі на цю дату
    public async Task<IReadOnlyList<TimesheetDayPerPersonCurrentStateDto>> GetDailyTimesheetAsync(
    DateOnly date,
    string? search,
    EnrollmentKind? enrollmentKind = null,
    bool activeOnly = true,
    CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var personsQ = db.PersonRead.AsNoTracking().AsQueryable();

        if (activeOnly)
            personsQ = personsQ.Where(p => p.Lifecycle == PersonLifecycle.Enrolled);

        // “активний на дату” (та сама логіка, що в GetActiveEntriesForTimesheetOnDateAsync)
        personsQ = personsQ.Where(p =>
            p.EnrolledAt.HasValue &&
            p.EnrolledAt.Value <= date &&
            (!p.ExcludedAt.HasValue || p.ExcludedAt.Value >= date));

        if (enrollmentKind is not null)
            personsQ = personsQ.Where(p => p.EnrollmentKind == enrollmentKind);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();

            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .ThenBy(p => p.FullName)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.Position,
                p.EnrollmentKind,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return [];

        var personIds = persons.Select(x => x.Id).ToArray();

        // активні записи на дату для вибраних осіб (2 лейни)
        var entries = await db.TimesheetEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                && personIds.Contains(e.PersonId)
                && e.From <= date
                && (!e.To.HasValue || e.To.Value >= date))
            .ToListAsync(ct);

        // через інваріант “без перетинів в lane” тут максимум 1 запис на (PersonId,Lane),
        // але підстрахуємось
        var map = entries
            .GroupBy(e => (e.PersonId, e.Lane))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.From).ThenByDescending(x => x.Id).First());

        var result = new List<TimesheetDayPerPersonCurrentStateDto>(persons.Count);

        foreach (var p in persons)
        {
            map.TryGetValue((p.Id, TimesheetLane.Main), out var main);
            map.TryGetValue((p.Id, TimesheetLane.Task), out var task);

            result.Add(new TimesheetDayPerPersonCurrentStateDto(
                PersonId: p.Id,
                FullName: p.FullName ?? "",
                RNOKPP: p.Rnokpp ?? "",
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                MainEntryId: main?.Id,
                MainCode: main?.Code ?? "НБ",
                TaskEntryId: task?.Id,
                TaskCode: task?.Code ?? ""
            ));
        }

        return result;
    }

    // Отримати всі записи табеля певної особи за період
    public async Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .Where(x => x.From <= to && (x.To == null || x.To >= from)) // overlap with [from..to]
            .OrderBy(x => x.Lane)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    // “Стан на дату” для всіх, хто в табелі на цю дату (через EnrolledAt/ExcludedAt).
    public async Task<IReadOnlyList<TimesheetEntry>> GetActiveEntriesForTimesheetOnDateAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // “в табелі на дату” = по EnrolledAt/ExcludedAt (це виправляє кейс з Reserved)
        var activePersonIds = await db.PersonRead
            .AsNoTracking()
            .Where(p =>
                p.EnrolledAt != null &&
                p.EnrolledAt <= date &&
                (p.ExcludedAt == null || p.ExcludedAt >= date))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (activePersonIds.Count == 0)
            return [];

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => activePersonIds.Contains(x.PersonId))
            .Where(x => x.From <= date && (x.To == null || x.To >= date))
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.Lane)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    // Отримати запис табеля за Id
    public async Task<TimesheetEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entryId && !x.IsDeleted, ct);
    }

    // Забезпечити відкриття табеля на дату зарахування
    public async Task EnsureOpenedOnEnrollAsync(Guid personId, DateOnly enrollDate, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);

        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Шукаємо запис у MAIN lane, який перекриває enrollDate.
        // Якщо він є — вважаємо, що “табель вже відкритий” (нічого не додаємо).
        var overlapMain = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .Where(x => x.From <= enrollDate && (x.To == null || x.To >= enrollDate))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (overlapMain is not null)
        {
            // Якщо це відкритий попередній запис, який почався раніше —
            // це означає “старий період не закритий”. Для MVP:
            //  - якщо To=null і From < enrollDate — закриваємо на enrollDate-1
            //  - і продовжуємо відкривати новий дефолтний запис
            if (overlapMain.To is null && overlapMain.From < enrollDate)
            {
                overlapMain.To = enrollDate.AddDays(-1);
                overlapMain.UpdatedBy = author.Trim();
                overlapMain.UpdatedAtUtc = nowUtc;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                // 1) запис починається цього ж дня (або пізніше) — табель вже “є”
                // 2) або To != null і перекриває enrollDate — це бізнес-помилка даних
                // Для MVP: якщо From == enrollDate — просто нічого не робимо.
                if (overlapMain.From == enrollDate)
                    return;

                throw new InvalidOperationException(
                    "Неможливо відкрити табель при зарахуванні: існує запис MAIN, що перекриває дату зарахування.");
            }
        }

        // Створюємо дефолтний запис “В районі” (код 30) з дати зарахування
        // (без To, open-ended)
        await EnsureNoOverlapAsync(
            db,
            entryId: null,
            personId: personId,
            lane: TimesheetLane.Main,
            from: enrollDate,
            to: null,
            ct: ct);

        var id = Guid.NewGuid();

        db.TimesheetEntries.Add(new TimesheetEntry
        {
            Id = id,
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "30",
            From = enrollDate,
            To = null,
            Reference = "Auto: Enroll",
            Note = null,
            CreatedBy = author.Trim(),
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        });

        await db.SaveChangesAsync(ct);
    }

    // Забезпечити закриття табеля на дату виключення
    public async Task EnsureClosedOnExcludeAsync(Guid personId, DateOnly closeTo, string? reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);

        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Обрізати всі записи, які “залізли” за дату виключення (або були open-ended)
        var toClamp = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .Where(x => x.From <= closeTo)
            .Where(x => x.To == null || x.To > closeTo)
            .ToListAsync(ct);

        foreach (var e in toClamp)
        {
            e.To = closeTo;
            e.UpdatedBy = author.Trim();
            e.UpdatedAtUtc = nowUtc;
        }

        // 2) Прибрати майбутні записи (From > closeTo) — щоб після виключення не висіли “плани”
        //    (якщо захочеш залишати для аудиту — скажеш, зробимо окремий прапор/режим)
        var future = await db.TimesheetEntries
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .Where(x => x.From > closeTo)
            .ToListAsync(ct);

        if (future.Count > 0)
        {
            var r = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            var msg = r is null
                ? "Auto-deleted: person excluded"
                : $"Auto-deleted: person excluded ({r})";

            foreach (var e in future)
            {
                e.IsDeleted = true;
                e.DeletedBy = author.Trim();
                e.DeletedAtUtc = nowUtc;
                e.DeleteReason = msg;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // Створити новий запис табеля
    public async Task<Guid> CreateEntryAsync(Guid personId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureCode(code);

        if (to is DateOnly t && t < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        await EnsureNoOverlapAsync(db, entryId: null, personId, lane, from, to, ct);

        var id = Guid.NewGuid();

        var entry = new TimesheetEntry
        {
            Id = id,
            PersonId = personId,
            Lane = lane,
            Code = code.Trim(),
            From = from,
            To = to,
            Reference = Normalize(reference),
            Note = Normalize(note),
            CreatedBy = author.Trim(),
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };

        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return id;
    }

    // Оновити існуючий запис табеля
    public async Task UpdateEntryAsync(Guid entryId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureCode(code);

        if (entryId == Guid.Empty)
            throw new ArgumentException("EntryId is required.", nameof(entryId));

        if (to is DateOnly t && t < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entry = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == entryId, ct);

        if (entry is null || entry.IsDeleted)
            throw new InvalidOperationException("Entry not found.");

        await EnsureNoOverlapAsync(db, entryId, entry.PersonId, lane, from, to, ct);

        entry.Lane = lane;
        entry.Code = code.Trim();
        entry.From = from;
        entry.To = to;
        entry.Reference = Normalize(reference);
        entry.Note = Normalize(note);
        entry.UpdatedBy = author.Trim();
        entry.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    // Видалити запис табеля (soft delete)
    public async Task DeleteEntryAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);

        if (entryId == Guid.Empty)
            throw new ArgumentException("EntryId is required.", nameof(entryId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entry = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == entryId, ct);

        if (entry is null || entry.IsDeleted)
            return;

        entry.IsDeleted = true;
        entry.DeletedBy = author.Trim();
        entry.DeletedAtUtc = nowUtc;
        entry.DeleteReason = reason.Trim();

        await db.SaveChangesAsync(ct);
    }

    // =========================
    // Overlap rule
    // =========================

    private static async Task EnsureNoOverlapAsync(AppDbContext db, Guid? entryId, Guid personId, TimesheetLane lane,
        DateOnly from, DateOnly? to, CancellationToken ct)
    {
        var q = db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.Lane == lane && !x.IsDeleted);

        if (entryId is Guid id)
            q = q.Where(x => x.Id != id);

        // overlap with [from..to] (to null = infinity)
        if (to is DateOnly t)
        {
            q = q.Where(x => x.From <= t && (x.To == null || x.To >= from));
        }
        else
        {
            q = q.Where(x => x.To == null || x.To >= from);
        }

        var hasOverlap = await q.AnyAsync(ct);
        if (hasOverlap)
            throw new InvalidOperationException("Запис перетинається з існуючим записом у цій lane. Спочатку відредагуйте/закрийте попередній запис.");
    }

    private static List<MonthlyTimesheetDayDto> BuildDaysForMonth(
      int year,
      int month,
      DateOnly monthStart,
      DateOnly monthEnd,
      List<TimesheetEntry>? entries)
    {
        const string NotInTimesheet = "НБ";

        var daysInMonth = DateTime.DaysInMonth(year, month);

        var main = new Dictionary<int, MonthlyTimesheetDayDto>(daysInMonth);
        var task = new Dictionary<int, MonthlyTimesheetDayDto>(daysInMonth);

        // 1) base defaults (entries-only)
        for (var d = 1; d <= daysInMonth; d++)
        {
            main[d] = new MonthlyTimesheetDayDto(
                EntryId: null,
                Day: d,
                Lane: TimesheetLane.Main,
                Code: NotInTimesheet);

            task[d] = new MonthlyTimesheetDayDto(
                EntryId: null,
                Day: d,
                Lane: TimesheetLane.Task,
                Code: "");
        }

        // 2) overlay entries
        if (entries is not null && entries.Count > 0)
        {
            foreach (var e in entries
                .OrderBy(x => x.Lane)
                .ThenBy(x => x.From)
                .ThenBy(x => x.Id))
            {
                var start = e.From < monthStart ? monthStart : e.From;
                var end = e.To is null
                    ? monthEnd
                    : (e.To.Value > monthEnd ? monthEnd : e.To.Value);

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var day = date.Day;

                    if (e.Lane == TimesheetLane.Main)
                    {
                        main[day] = new MonthlyTimesheetDayDto(e.Id, day, TimesheetLane.Main, e.Code);
                    }
                    else if (e.Lane == TimesheetLane.Task)
                    {
                        task[day] = new MonthlyTimesheetDayDto(e.Id, day, TimesheetLane.Task, e.Code);
                    }
                }
            }
        }

        // 3) flatten
        var res = new List<MonthlyTimesheetDayDto>(daysInMonth * 2);
        for (var d = 1; d <= daysInMonth; d++)
        {
            res.Add(main[d]);
            res.Add(task[d]);
        }

        return res;
    }


    private static void EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));
    }

    private static void EnsureCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
