//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetRepositoryTests : IAsyncLifetime
{
    private SqliteTestDb _db = default!;
    private TimesheetRepository _repo = default!;

    private static readonly DateTime NowUtc = new(2026, 01, 20, 10, 0, 0, DateTimeKind.Utc);
    private static MonthlyTimesheetDayDto Cell(MonthlyTimesheetReadModelDto ts, int day, TimesheetLane lane)
    => ts.Days.Single(x => x.Day == day && x.Lane == lane);

    public Task InitializeAsync()
    {
        _db = new SqliteTestDb();
        _repo = new TimesheetRepository(_db.Factory);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
        => await _db.DisposeAsync();

    // =========================
    // Create / Read
    // =========================

    [Fact]
    public async Task CreateEntryAsync_should_persist_and_GetEntryByIdAsync_should_return_it()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var entryId = await _repo.CreateEntryAsync(
            personId: personId,
            lane: TimesheetLane.Main,
            code: "30",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: "ref",
            note: "note",
            author: "system",
            nowUtc: nowUtc);

        var loaded = await _repo.GetEntryByIdAsync(entryId);

        Assert.NotNull(loaded);
        Assert.Equal(entryId, loaded!.Id);
        Assert.Equal(personId, loaded.PersonId);
        Assert.Equal(TimesheetLane.Main, loaded.Lane);
        Assert.Equal("30", loaded.Code);
        Assert.Equal(new DateOnly(2026, 01, 10), loaded.From);
        Assert.Null(loaded.To);
        Assert.Equal("ref", loaded.Reference);
        Assert.Equal("note", loaded.Note);
        Assert.Equal("system", loaded.CreatedBy);
        Assert.Equal(nowUtc, loaded.CreatedAtUtc);
        Assert.False(loaded.IsDeleted);
    }

    [Fact]
    public async Task CreateEntryAsync_should_throw_when_to_is_less_than_from()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repo.CreateEntryAsync(
            personId: Guid.NewGuid(),
            lane: TimesheetLane.Main,
            code: "30",
            from: new DateOnly(2026, 01, 10),
            to: new DateOnly(2026, 01, 09),
            reference: null,
            note: null,
            author: "system",
            nowUtc: DateTime.UtcNow));
    }

    [Fact]
    public async Task CreateEntryAsync_should_throw_when_author_or_code_missing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _repo.CreateEntryAsync(
            personId: Guid.NewGuid(),
            lane: TimesheetLane.Main,
            code: "30",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: null,
            note: null,
            author: "",
            nowUtc: DateTime.UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(() => _repo.CreateEntryAsync(
            personId: Guid.NewGuid(),
            lane: TimesheetLane.Main,
            code: " ",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: null,
            note: null,
            author: "system",
            nowUtc: DateTime.UtcNow));
    }

    // =========================
    // Overlap rule
    // =========================

    [Fact]
    public async Task CreateEntryAsync_should_throw_on_overlap_in_same_lane()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // existing: [10..12]
        _ = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 10),
            new DateOnly(2026, 01, 12),
            null, null, "system", nowUtc);

        // candidate overlaps: [11..13]
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 11),
            new DateOnly(2026, 01, 13),
            null, null, "system", nowUtc));

        Assert.Contains("перетинається", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEntryAsync_should_allow_same_period_in_different_lane()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // Main: [10..12]
        _ = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 10),
            new DateOnly(2026, 01, 12),
            null, null, "system", nowUtc);

        // Task: [10..12] - OK (different lane)
        var id2 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 10),
            new DateOnly(2026, 01, 12),
            null, null, "system", nowUtc);

        var loaded = await _repo.GetEntryByIdAsync(id2);
        Assert.NotNull(loaded);
        Assert.Equal(TimesheetLane.Task, loaded!.Lane);
    }

    [Fact]
    public async Task UpdateEntryAsync_should_throw_on_overlap_with_other_existing_entry()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // e1: [01..05]
        var e1 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 01),
            new DateOnly(2026, 01, 05),
            null, null, "system", nowUtc);

        // e2: [10..12]
        var e2 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 10),
            new DateOnly(2026, 01, 12),
            null, null, "system", nowUtc);

        // try update e2 to overlap e1: [04..11]
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateEntryAsync(
            entryId: e2,
            lane: TimesheetLane.Task,
            code: "BTGr",
            from: new DateOnly(2026, 01, 04),
            to: new DateOnly(2026, 01, 11),
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc.AddMinutes(1)));
    }

    // =========================
    // Update
    // =========================

    [Fact]
    public async Task UpdateEntryAsync_should_update_fields_and_set_updated_audit()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var entryId = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 10),
            null,
            "r1", "n1", "system", nowUtc);

        var now2 = nowUtc.AddMinutes(5);

        await _repo.UpdateEntryAsync(
            entryId: entryId,
            lane: TimesheetLane.Main,
            code: "F100",
            from: new DateOnly(2026, 01, 11),
            to: new DateOnly(2026, 01, 12),
            reference: "r2",
            note: "n2",
            author: "operator",
            nowUtc: now2);

        var loaded = await _repo.GetEntryByIdAsync(entryId);

        Assert.NotNull(loaded);
        Assert.Equal("F100", loaded!.Code);
        Assert.Equal(new DateOnly(2026, 01, 11), loaded.From);
        Assert.Equal(new DateOnly(2026, 01, 12), loaded.To);
        Assert.Equal("r2", loaded.Reference);
        Assert.Equal("n2", loaded.Note);
        Assert.Equal("operator", loaded.UpdatedBy);
        Assert.Equal(now2, loaded.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateEntryAsync_should_throw_when_entry_not_found_or_deleted()
    {
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateEntryAsync(
            entryId: Guid.NewGuid(),
            lane: TimesheetLane.Main,
            code: "30",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc));

        var personId = Guid.NewGuid();
        var id = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 10),
            null, null, null, "system", nowUtc);

        await _repo.DeleteEntryAsync(id, "reason", "system", nowUtc.AddMinutes(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateEntryAsync(
            entryId: id,
            lane: TimesheetLane.Main,
            code: "31",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc.AddMinutes(2)));
    }

    // =========================
    // Delete (soft)
    // =========================

    [Fact]
    public async Task DeleteEntryAsync_should_soft_delete_and_GetEntryByIdAsync_should_hide_deleted()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var entryId = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 10),
            null, null, null, "system", nowUtc);

        await _repo.DeleteEntryAsync(
            entryId: entryId,
            reason: "correction",
            author: "operator",
            nowUtc: nowUtc.AddMinutes(1));

        var visible = await _repo.GetEntryByIdAsync(entryId);
        Assert.Null(visible);

        // but in DB it exists and marked deleted
        await using var db = await _db.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);

        Assert.True(raw.IsDeleted);
        Assert.Equal("operator", raw.DeletedBy);
        Assert.Equal("correction", raw.DeleteReason);
        Assert.Equal(nowUtc.AddMinutes(1), raw.DeletedAtUtc);
    }

    [Fact]
    public async Task DeleteEntryAsync_should_be_idempotent()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var entryId = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 10),
            null, null, null, "system", nowUtc);

        // delete twice - should not throw
        await _repo.DeleteEntryAsync(entryId, "correction", "operator", nowUtc.AddMinutes(1));
        await _repo.DeleteEntryAsync(entryId, "correction2", "operator2", nowUtc.AddMinutes(2));

        // still hidden
        var visible = await _repo.GetEntryByIdAsync(entryId);
        Assert.Null(visible);

        await using var db = await _db.Factory.CreateDbContextAsync();
        var raw = await db.TimesheetEntries.AsNoTracking().SingleAsync(x => x.Id == entryId);

        Assert.True(raw.IsDeleted);
        // IMPORTANT: your implementation returns early if already deleted, so these stay as first delete.
        Assert.Equal("operator", raw.DeletedBy);
        Assert.Equal("correction", raw.DeleteReason);
        Assert.Equal(nowUtc.AddMinutes(1), raw.DeletedAtUtc);
    }

    // =========================
    // Queries
    // =========================

    [Fact]
    public async Task GetPersonEntriesAsync_should_return_entries_overlapping_range_sorted()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // Main lane (без перетинів між собою)
        var e1 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "A",
            new DateOnly(2026, 01, 01), new DateOnly(2026, 01, 05),
            null, null, "system", nowUtc);

        var e2 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Main, "B",
            new DateOnly(2026, 01, 06), new DateOnly(2026, 01, 10),
            null, null, "system", nowUtc);

        // Task lane (може перетинатися з Main, але не з Task в Task)
        var e3 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "T1",
            new DateOnly(2026, 01, 03), new DateOnly(2026, 01, 04),
            null, null, "system", nowUtc);

        var e4 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "T2",
            new DateOnly(2026, 01, 11), null, // open-ended, не перетинається з e3
            null, null, "system", nowUtc);

        // Запитний діапазон, який "зачіпає" кілька записів
        var from = new DateOnly(2026, 01, 04);
        var to = new DateOnly(2026, 01, 11);

        var rows = await _repo.GetPersonEntriesAsync(personId, from, to);

        // Маємо отримати 4 записи: e1 (зачіпає 04..05), e2 (06..10), e3 (04), e4 (11..∞)
        Assert.Equal(4, rows.Count);

        // Перевіряємо сортування: Lane -> From -> Id
        // Тому в expected робимо так само:
        var expected = rows
            .OrderBy(x => x.Lane)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .ToArray();

        Assert.Equal(expected, rows.Select(x => x.Id).ToArray());

        // І додатково — що це саме ті entryId
        var ids = rows.Select(x => x.Id).ToHashSet();
        Assert.Contains(e1, ids);
        Assert.Contains(e2, ids);
        Assert.Contains(e3, ids);
        Assert.Contains(e4, ids);
    }

    [Fact]
    public async Task GetActiveEntriesForTimesheetOnDateAsync_should_return_only_active_persons_entries_on_date()
    {
        var date = new DateOnly(2026, 01, 15);
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var activePersonId = Guid.NewGuid();
        var reservedPersonId = Guid.NewGuid();

        // seed PersonRead for active vs reserved
        await using (var db = await _db.Factory.CreateDbContextAsync())
        {
            db.PersonRead.Add(new PersonReadModel
            {
                Id = activePersonId,
                FullName = "Active",
                Rnokpp = "1111111111",
                Lifecycle = PersonLifecycle.Enrolled,
                EnrollmentKind = EnrollmentKind.Unit,
                EnrolledAt = new DateOnly(2026, 01, 10),
                ExcludedAt = null,
                UpdatedAtUtc = nowUtc
            });

            db.PersonRead.Add(new PersonReadModel
            {
                Id = reservedPersonId,
                FullName = "Reserved",
                Rnokpp = "2222222222",
                Lifecycle = PersonLifecycle.Reserved,
                EnrollmentKind = null,
                EnrolledAt = null,
                ExcludedAt = null,
                UpdatedAtUtc = nowUtc
            });

            await db.SaveChangesAsync();
        }

        // seed entries for both persons
        _ = await _repo.CreateEntryAsync(activePersonId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 01), null, null, null, "system", nowUtc);

        _ = await _repo.CreateEntryAsync(reservedPersonId, TimesheetLane.Main, "30",
            new DateOnly(2026, 01, 01), null, null, null, "system", nowUtc);

        var rows = await _repo.GetActiveEntriesForTimesheetOnDateAsync(date);

        Assert.Single(rows);
        Assert.Equal(activePersonId, rows[0].PersonId);
        Assert.Equal("30", rows[0].Code);
    }

    [Fact]
    public async Task GetActiveEntriesForTimesheetOnDateAsync_should_return_empty_when_no_active_persons()
    {
        // No PersonRead seeded
        var rows = await _repo.GetActiveEntriesForTimesheetOnDateAsync(new DateOnly(2026, 01, 15));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task CreateEntryAsync_should_throw_when_existing_open_ended_entry_overlaps_in_same_lane()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // existing: [10..∞)
        _ = await _repo.CreateEntryAsync(
            personId: personId,
            lane: TimesheetLane.Task,
            code: "BTGr",
            from: new DateOnly(2026, 01, 10),
            to: null,
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc);

        // candidate: [12..15] overlaps with open-ended
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.CreateEntryAsync(
            personId: personId,
            lane: TimesheetLane.Task,
            code: "BTGr",
            from: new DateOnly(2026, 01, 12),
            to: new DateOnly(2026, 01, 15),
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc));

        Assert.Contains("перетинається", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateEntryAsync_should_throw_when_updating_to_open_ended_overlaps_another_entry_in_same_lane()
    {
        var personId = Guid.NewGuid();
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // e1: [01..05]
        var e1 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 01),
            new DateOnly(2026, 01, 05),
            null, null, "system", nowUtc);

        // e2: [10..12]
        var e2 = await _repo.CreateEntryAsync(
            personId, TimesheetLane.Task, "BTGr",
            new DateOnly(2026, 01, 10),
            new DateOnly(2026, 01, 12),
            null, null, "system", nowUtc);

        // try update e1 -> [04..∞) overlaps with e2 (because infinity crosses 10..12)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateEntryAsync(
            entryId: e1,
            lane: TimesheetLane.Task,
            code: "BTGr",
            from: new DateOnly(2026, 01, 04),
            to: null,
            reference: null,
            note: null,
            author: "system",
            nowUtc: nowUtc.AddMinutes(1)));

        Assert.Contains("перетинається", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnsureOpenedOnEnrollAsync_should_create_default_Main_entry_30_from_enrollDate()
    {
        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 01, 10);
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        await _repo.EnsureOpenedOnEnrollAsync(
            personId: personId,
            enrollDate: enrollDate,
            author: "system",
            nowUtc: nowUtc);

        await using var db = await _db.Factory.CreateDbContextAsync();

        var entries = await db.Set<TimesheetEntry>()
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .ToListAsync();

        var main = entries
            .Where(x => x.Lane == TimesheetLane.Main)
            .ToList();

        Assert.Single(main);

        var e = main[0];
        Assert.Equal("30", e.Code);
        Assert.Equal(enrollDate, e.From);
        Assert.Null(e.To);
    }

    [Fact]
    public async Task EnsureOpenedOnEnrollAsync_should_be_idempotent_when_default_entry_already_exists()
    {
        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 01, 10);
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var existingId = Guid.NewGuid();

        await using (var db = await _db.Factory.CreateDbContextAsync())
        {
            db.Set<TimesheetEntry>().Add(new TimesheetEntry
            {
                Id = existingId,
                PersonId = personId,
                Lane = TimesheetLane.Main,
                Code = "30",
                From = enrollDate,
                To = null,
                Reference = null,
                Note = null,
                CreatedBy = "seed",
                CreatedAtUtc = nowUtc,
                IsDeleted = false
            });

            await db.SaveChangesAsync();
        }

        // act: call Ensure again
        await _repo.EnsureOpenedOnEnrollAsync(
            personId: personId,
            enrollDate: enrollDate,
            author: "system",
            nowUtc: nowUtc);

        await using var db2 = await _db.Factory.CreateDbContextAsync();

        var main = await db2.Set<TimesheetEntry>()
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted && x.Lane == TimesheetLane.Main)
            .OrderBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync();

        Assert.Single(main);
        Assert.Equal(existingId, main[0].Id); // не створюємо дубль
        Assert.Equal("30", main[0].Code);
        Assert.Equal(enrollDate, main[0].From);
        Assert.Null(main[0].To);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_should_close_all_open_entries_for_person_by_setting_To()
    {
        var personId = Guid.NewGuid();
        var enrollDate = new DateOnly(2026, 01, 10);
        var closeTo = new DateOnly(2026, 01, 15);
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        var mainId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await using (var db = await _db.Factory.CreateDbContextAsync())
        {
            db.Set<TimesheetEntry>().AddRange(
                new TimesheetEntry
                {
                    Id = mainId,
                    PersonId = personId,
                    Lane = TimesheetLane.Main,
                    Code = "30",
                    From = enrollDate,
                    To = null,
                    CreatedBy = "seed",
                    CreatedAtUtc = nowUtc,
                    IsDeleted = false
                },
                new TimesheetEntry
                {
                    Id = taskId,
                    PersonId = personId,
                    Lane = TimesheetLane.Task,
                    Code = "RPT-1",
                    From = enrollDate.AddDays(1),
                    To = null,
                    CreatedBy = "seed",
                    CreatedAtUtc = nowUtc,
                    IsDeleted = false
                }
            );

            await db.SaveChangesAsync();
        }

        await _repo.EnsureClosedOnExcludeAsync(
            personId: personId,
            closeTo: closeTo,
            reason: "Exclude",
            author: "system",
            nowUtc: nowUtc);

        await using var db2 = await _db.Factory.CreateDbContextAsync();

        var loadedMain = await db2.Set<TimesheetEntry>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == mainId);

        var loadedTask = await db2.Set<TimesheetEntry>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == taskId);

        Assert.Equal(closeTo, loadedMain.To);
        Assert.Equal(closeTo, loadedTask.To);
    }

    [Fact]
    public async Task EnsureClosedOnExcludeAsync_should_be_idempotent_when_no_open_entries_exist()
    {
        var personId = Guid.NewGuid();
        var closeTo = new DateOnly(2026, 01, 15);
        var nowUtc = new DateTime(2026, 01, 19, 10, 0, 0, DateTimeKind.Utc);

        // нічого не сідімо — method має “тихо” пройти
        await _repo.EnsureClosedOnExcludeAsync(
            personId: personId,
            closeTo: closeTo,
            reason: "Exclude",
            author: "system",
            nowUtc: nowUtc);

        await using var db = await _db.Factory.CreateDbContextAsync();
        var any = await db.Set<TimesheetEntry>()
            .AsNoTracking()
            .AnyAsync(x => x.PersonId == personId);

        Assert.False(any);
    }

    [Fact]
    public async Task GetMonthlyTimesheetAsync_should_return_only_persons_active_in_month_apply_search_and_sort()
    {
        await using var db = await _db.Factory.CreateDbContextAsync();

        // Jan 2026 window

        // A: active in Jan, PositionSort=20
        var pA = new PersonReadModel
        {
            Id = Guid.NewGuid(),
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "111",
            LastName = "A",
            FirstName = "A",
            FullName = "A Person",
            Rank = "Солдат",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 20,
            Position = "Стрілець",
            EnrolledAt = new DateOnly(2026, 01, 05),
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = NowUtc
        };

        // B: active in Jan, PositionSort=10 => must come first
        var pB = new PersonReadModel
        {
            Id = Guid.NewGuid(),
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "222",
            LastName = "B",
            FirstName = "B",
            FullName = "B Person",
            Rank = "Сержант",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 10,
            Position = "Навідник",
            EnrolledAt = new DateOnly(2026, 01, 10),
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = NowUtc
        };

        // C: NOT active in Jan (enrolled in Feb) -> must be excluded
        var pC = new PersonReadModel
        {
            Id = Guid.NewGuid(),
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "333",
            LastName = "C",
            FirstName = "C",
            FullName = "C Person",
            Rank = "Солдат",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 5,
            Position = "Стрілець",
            EnrolledAt = new DateOnly(2026, 02, 01),
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = NowUtc
        };

        db.PersonRead.AddRange(pA, pB, pC);

        db.TimesheetEntries.AddRange(
            new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                PersonId = pA.Id,
                Lane = TimesheetLane.Main,
                Code = "30",
                From = pA.EnrolledAt!.Value,
                To = null,
                CreatedBy = "seed",
                CreatedAtUtc = NowUtc,
                IsDeleted = false
            },
            new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                PersonId = pB.Id,
                Lane = TimesheetLane.Main,
                Code = "30",
                From = pB.EnrolledAt!.Value,
                To = null,
                CreatedBy = "seed",
                CreatedAtUtc = NowUtc,
                IsDeleted = false
            }
        );
        await db.SaveChangesAsync();

        // act: no search
        var rows = await _repo.GetMonthlyTimesheetAsync(2026, 1, search: null);

        // assert: only A and B, sorted by EnrollmentKind then PositionSort
        Assert.Equal(2, rows.Count);
        Assert.Equal(pB.Id, rows[0].PersonId);
        Assert.Equal(pA.Id, rows[1].PersonId);

        // days count must match month length * 2 lanes
        Assert.Equal(31 * 2, rows[0].Timesheet!.Days.Count);

        // A enrolled at 05.01 => day1 Main is НБ, day5 Main is 30
        var a = rows.Single(x => x.PersonId == pA.Id).Timesheet!;
        Assert.Equal("НБ", Cell(a, 1, TimesheetLane.Main).Code);
        Assert.Equal("30", Cell(a, 5, TimesheetLane.Main).Code);

        // search filters by rnokpp/fullname
        var onlyA = await _repo.GetMonthlyTimesheetAsync(2026, 1, search: "111");
        Assert.Single(onlyA);
        Assert.Equal(pA.Id, onlyA[0].PersonId);

        var none = await _repo.GetMonthlyTimesheetAsync(2026, 1, search: "no-match");
        Assert.Empty(none);
    }

    [Fact]
    public async Task GetMonthlyTimesheetAsync_when_monthly_read_model_exists_should_take_UpdatedAtUtc_from_it_but_days_are_built()
    {
        await using var db = await _db.Factory.CreateDbContextAsync();

        var personId = Guid.NewGuid();

        db.PersonRead.Add(new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "777",
            LastName = "Ivanov",
            FirstName = "Ivan",
            FullName = "Ivanov Ivan",
            Rank = "Солдат",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 1,
            Position = "Стрілець",
            EnrolledAt = new DateOnly(2026, 01, 01),
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = NowUtc
        });

        var rmUpdated = new DateTime(2026, 01, 19, 9, 0, 0, DateTimeKind.Utc);

        db.Set<MonthlyTimesheetReadModel>().Add(new MonthlyTimesheetReadModel
        {
            PersonId = personId,
            Year = 2026,
            Month = 1,
            Version = 5,
            UpdatedAtUtc = rmUpdated
            // Days можна не заповнювати — репо їх зараз не читає
        });

        // entry that overrides main on 10..12
        db.Set<TimesheetEntry>().Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "100",
            From = new DateOnly(2026, 01, 10),
            To = new DateOnly(2026, 01, 12),
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        });

        db.Set<TimesheetEntry>().Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "30",
            From = new DateOnly(2026, 01, 01),
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        });

        await db.SaveChangesAsync();

        var rows = await _repo.GetMonthlyTimesheetAsync(2026, 1, search: null);
        Assert.Single(rows);

        var ts = rows[0].Timesheet!;
        Assert.Equal(rmUpdated, ts.UpdatedAtUtc);

        // day 9 main is default 30 (in timesheet), day 10 main overridden to 100
        Assert.Equal("30", Cell(ts, 9, TimesheetLane.Main).Code);
        Assert.Equal("100", Cell(ts, 10, TimesheetLane.Main).Code);

        // task lane default empty
        Assert.Equal("", Cell(ts, 10, TimesheetLane.Task).Code);
    }

    [Fact]
    public async Task GetMonthlyTimesheetAsync_when_read_model_missing_should_build_days_from_enroll_exclude_and_entries()
    {
        await using var db = await _db.Factory.CreateDbContextAsync();

        var personId = Guid.NewGuid();

        db.PersonRead.Add(new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "888",
            LastName = "Petrov",
            FirstName = "Petr",
            FullName = "Petrov Petr",
            Rank = "Солдат",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 1,
            Position = "Стрілець",
            EnrolledAt = new DateOnly(2026, 01, 10),
            ExcludedAt = new DateOnly(2026, 01, 20),
            Version = 1,
            UpdatedAtUtc = NowUtc
        });

        db.TimesheetEntries.Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "30",
            From = new DateOnly(2026, 01, 10),
            To = new DateOnly(2026, 01, 20),
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        });

        await db.SaveChangesAsync();

        var rows = await _repo.GetMonthlyTimesheetAsync(2026, 1, search: null);
        Assert.Single(rows);

        var ts = rows[0].Timesheet!;
        Assert.Equal(DateTime.MinValue, ts.UpdatedAtUtc);

        // before enroll => НБ
        Assert.Equal("НБ", Cell(ts, 9, TimesheetLane.Main).Code);

        // in timesheet => 30
        Assert.Equal("30", Cell(ts, 10, TimesheetLane.Main).Code);
        Assert.Equal("30", Cell(ts, 20, TimesheetLane.Main).Code);

        // after exclude => НБ
        Assert.Equal("НБ", Cell(ts, 21, TimesheetLane.Main).Code);

        // task lane is always empty by default
        Assert.Equal("", Cell(ts, 10, TimesheetLane.Task).Code);
        Assert.Equal("", Cell(ts, 21, TimesheetLane.Task).Code);
    }

    [Fact]
    public async Task GetMonthlyTimesheetAsync_should_show_two_enrollment_periods_within_same_month()
    {
        // arrange
        await using var db = await _db.Factory.CreateDbContextAsync();

        var personId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        // Period #1: 05..08
        var enroll1 = new DateOnly(year, month, 5);
        var exclude1 = new DateOnly(year, month, 8);

        // Period #2: from 12..
        var enroll2 = new DateOnly(year, month, 12);

        // IMPORTANT: PersonReadModel після повторного зарахування містить тільки "останнє" EnrolledAt
        db.PersonRead.Add(new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 1,
            Rnokpp = "9999999999",
            LastName = "Іванов",
            FirstName = "Іван",
            FullName = "Іванов Іван Іванович",
            Rank = "Солдат",
            Position = "Стрілець",
            EnrolledAt = enroll2,
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = NowUtc
        });

        // TimesheetEntry період #1 (закритий на дату виключення)
        db.TimesheetEntries.Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "30",
            From = enroll1,
            To = exclude1,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        });

        // TimesheetEntry період #2 (open-ended)
        db.TimesheetEntries.Add(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Lane = TimesheetLane.Main,
            Code = "30",
            From = enroll2,
            To = null,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        });

        await db.SaveChangesAsync();

        // act
        var rows = await _repo.GetMonthlyTimesheetAsync(year, month, search: null);

        // assert
        Assert.Single(rows);
        var r = rows[0];

        string CodeAt(int day, TimesheetLane lane)
            => r.Timesheet!.Days.Single(x => x.Day == day && x.Lane == lane).Code;

        // Очікування:
        // - 6 число в 1-му періоді має бути "30"
        // - 10 число між періодами має бути "НБ"
        // - 15 число в 2-му періоді має бути "30"
        Assert.Equal("30", CodeAt(6, TimesheetLane.Main));
        Assert.Equal("НБ", CodeAt(10, TimesheetLane.Main));
        Assert.Equal("30", CodeAt(15, TimesheetLane.Main));
    }

    [Fact]
    public async Task GetMonthlyTimesheetAsync_when_person_active_but_no_main_entries_should_show_NB_everywhere_in_Main()
    {
        await using var db = await _db.Factory.CreateDbContextAsync();

        var personId = Guid.NewGuid();

        db.PersonRead.Add(new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = "000",
            LastName = "X",
            FirstName = "Y",
            FullName = "X Y",
            Rank = "Солдат",
            EnrollmentKind = EnrollmentKind.Unit,
            PositionSort = 1,
            Position = "Стрілець",
            EnrolledAt = new DateOnly(2026, 01, 10),
            ExcludedAt = new DateOnly(2026, 01, 20),
            Version = 1,
            UpdatedAtUtc = NowUtc
        });

        // IMPORTANT: no TimesheetEntries
        await db.SaveChangesAsync();

        var rows = await _repo.GetMonthlyTimesheetAsync(2026, 1, null);
        Assert.Single(rows);

        var ts = rows[0].Timesheet!;
        Assert.Equal("НБ", Cell(ts, 10, TimesheetLane.Main).Code);
        Assert.Equal("НБ", Cell(ts, 20, TimesheetLane.Main).Code);
        Assert.Equal("", Cell(ts, 10, TimesheetLane.Task).Code);
    }
}
