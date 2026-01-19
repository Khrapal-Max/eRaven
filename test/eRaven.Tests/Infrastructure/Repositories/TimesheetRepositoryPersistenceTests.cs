//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepositoryTests
//-----------------------------------------------------------------------------

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
}
