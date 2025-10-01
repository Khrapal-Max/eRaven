//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusReadServiceTests — інтеграційні сценарії з in-memory SQLite
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonStatusReadServiceTests
{
    private static Person NewPerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Test",
        FirstName = "User",
        MiddleName = "Case",
        Rnokpp = Guid.NewGuid().ToString("N").PadLeft(10, '0'),
        Rank = "прап.",
        CreatedUtc = DateTime.UtcNow,
        ModifiedUtc = DateTime.UtcNow
    };

    private static StatusKind NewKind(string code, string name, int order) => new()
    {
        Code = code,
        Name = name,
        Order = order,
        IsActive = true,
        Author = "test",
        Modified = DateTime.UtcNow
    };

    private static PersonStatus NewStatus(Guid personId, int kindId, DateTime openUtc, Guid? id = null, short sequence = 0) => new()
    {
        Id = id ?? Guid.NewGuid(),
        PersonId = personId,
        StatusKindId = kindId,
        OpenDate = openUtc,
        Sequence = sequence,
        IsActive = true,
        Author = "test",
        Modified = DateTime.UtcNow
    };

    private static PositionUnit NewPositionUnit() => new()
    {
        Id = Guid.NewGuid(),
        ShortName = "Pos",
        IsActived = true
    };

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    [Fact(DisplayName = "GetFirstPresenceUtcAsync: мінімум між першим \"В районі\" та призначенням")]
    public async Task GetFirstPresenceUtcAsync_Returns_MinBetweenStatusAndAssignment()
    {
        using var helper = new SqliteDbHelper();
        var svc = new PersonStatusReadService(helper.Factory);
        var ct = CancellationToken.None;
        var db = helper.Db;

        var person = NewPerson();
        var position = NewPositionUnit();
        var notPresent = NewKind("нб", "Не в строю", 900);
        var inDistrict = NewKind("30", "В районі", 10);

        db.AddRange(person, position, notPresent, inDistrict);
        await db.SaveChangesAsync(ct);

        var statusDate = Utc(2025, 9, 10);
        db.PersonStatuses.Add(NewStatus(person.Id, inDistrict.Id, statusDate));

        var assignmentDate = Utc(2025, 9, 5);
        db.PersonPositionAssignments.Add(new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = position.Id,
            OpenUtc = assignmentDate,
            CloseUtc = null,
            Note = "",
            Author = "test",
            ModifiedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        var firstPresence = await svc.GetFirstPresenceUtcAsync(person.Id, ct);

        Assert.Equal(assignmentDate, firstPresence);
    }

    [Fact(DisplayName = "GetActiveOnDateAsync: один момент → бере статус з меншим Order")]
    public async Task GetActiveOnDateAsync_Picks_HighestPriority_OnSameMoment()
    {
        using var helper = new SqliteDbHelper();
        var svc = new PersonStatusReadService(helper.Factory);
        var ct = CancellationToken.None;
        var db = helper.Db;

        var person = NewPerson();
        var notPresent = NewKind("нб", "Не в строю", 900);
        var high = NewKind("30", "В районі", 5);
        var low = NewKind("100", "В БР", 20);

        db.AddRange(person, notPresent, high, low);
        await db.SaveChangesAsync(ct);

        var openUtc = Utc(2025, 9, 12);
        db.PersonStatuses.AddRange(
            NewStatus(person.Id, low.Id, openUtc, Guid.Parse("00000000-0000-0000-0000-000000000010")),
            NewStatus(person.Id, high.Id, openUtc, Guid.Parse("00000000-0000-0000-0000-000000000009")));
        await db.SaveChangesAsync(ct);

        var active = await svc.GetActiveOnDateAsync(person.Id, Utc(2025, 9, 12, 23, 59, 59), ct);

        Assert.NotNull(active);
        Assert.Equal(high.Id, active!.StatusKindId);
        Assert.Equal(high.Order, active.StatusKind.Order);
    }

    [Fact(DisplayName = "GetActiveOnDateAsync: до першої присутності повертає 'нб'")]
    public async Task GetActiveOnDateAsync_BeforeFirstPresence_Returns_NotPresent()
    {
        using var helper = new SqliteDbHelper();
        var svc = new PersonStatusReadService(helper.Factory);
        var ct = CancellationToken.None;
        var db = helper.Db;

        var person = NewPerson();
        var notPresent = NewKind("нб", "Не в строю", 900);
        var inDistrict = NewKind("30", "В районі", 5);

        db.AddRange(person, notPresent, inDistrict);
        await db.SaveChangesAsync(ct);

        db.PersonStatuses.Add(NewStatus(person.Id, inDistrict.Id, Utc(2025, 9, 10)));
        await db.SaveChangesAsync(ct);

        var before = await svc.GetActiveOnDateAsync(person.Id, Utc(2025, 9, 9, 23, 59, 59), ct);

        Assert.NotNull(before);
        Assert.Equal(notPresent.Id, before!.StatusKindId);
        Assert.Equal("нб", before.StatusKind.Code, StringComparer.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "OrderForHistoryAsync: стабільна хронологія з tie-break по Order та Id")]
    public async Task OrderForHistoryAsync_ReturnsStableChronology()
    {
        using var helper = new SqliteDbHelper();
        var svc = new PersonStatusReadService(helper.Factory);
        var ct = CancellationToken.None;
        var db = helper.Db;

        var person = NewPerson();
        var notPresent = NewKind("нб", "Не в строю", 900);
        var kEarly = NewKind("10", "Черговий", 15);
        var kTieA = NewKind("20A", "Відряджений A", 5);
        var kTieB = NewKind("20B", "Відряджений B", 5);
        var kLate = NewKind("30", "В районі", 10);

        db.AddRange(person, notPresent, kEarly, kTieA, kTieB, kLate);
        await db.SaveChangesAsync(ct);

        var baseDay = Utc(2025, 9, 10);
        db.PersonStatuses.AddRange(
            NewStatus(person.Id, kEarly.Id, Utc(2025, 9, 9), Guid.Parse("00000000-0000-0000-0000-000000000001")),
            NewStatus(person.Id, kTieB.Id, baseDay, Guid.Parse("00000000-0000-0000-0000-000000000003")),
            NewStatus(person.Id, kTieA.Id, baseDay, Guid.Parse("00000000-0000-0000-0000-000000000002")),
            NewStatus(person.Id, kLate.Id, Utc(2025, 9, 11), Guid.Parse("00000000-0000-0000-0000-000000000004")));
        await db.SaveChangesAsync(ct);

        var ordered = await svc.OrderForHistoryAsync(person.Id, ct);

        Assert.Collection(ordered,
            s => Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), s.Id),
            s => Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), s.Id),
            s => Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000003"), s.Id),
            s => Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000004"), s.Id));
    }

    [Fact(DisplayName = "ResolveMonthAsync vs GetActiveOnDateAsync: одна й та сама доба")]
    public async Task TimesheetMonthAndStaffSnapshot_Agree_OnSameDay()
    {
        using var helper = new SqliteDbHelper();
        var svc = new PersonStatusReadService(helper.Factory);
        var ct = CancellationToken.None;
        var db = helper.Db;

        var person = NewPerson();
        var notPresent = NewKind("нб", "Не в строю", 900);
        var inDistrict = NewKind("30", "В районі", 5);
        var reserve = NewKind("100", "В БР", 20);

        db.AddRange(person, notPresent, inDistrict, reserve);
        await db.SaveChangesAsync(ct);

        db.PersonStatuses.AddRange(
            NewStatus(person.Id, inDistrict.Id, Utc(2025, 9, 10)),
            NewStatus(person.Id, reserve.Id, Utc(2025, 9, 15)));
        await db.SaveChangesAsync(ct);

        var month = await svc.ResolveMonthAsync(new[] { person.Id }, 2025, 9, ct);
        Assert.True(month.TryGetValue(person.Id, out var monthStatus));

        var fifteenthIndex = 15 - 1;
        var timesheetStatus = monthStatus!.Days[fifteenthIndex];
        var staffSnapshot = await svc.GetActiveOnDateAsync(person.Id, Utc(2025, 9, 15, 23, 59, 59), ct);

        Assert.NotNull(timesheetStatus);
        Assert.NotNull(staffSnapshot);
        Assert.Equal(timesheetStatus!.StatusKindId, staffSnapshot!.StatusKindId);
        Assert.Equal(timesheetStatus.StatusKind.Code, staffSnapshot.StatusKind.Code);

        var preIndex = 9 - 1;
        Assert.Null(monthStatus.Days[preIndex]);

        var beforeSnapshot = await svc.GetActiveOnDateAsync(person.Id, Utc(2025, 9, 9, 23, 59, 59), ct);
        Assert.NotNull(beforeSnapshot);
        Assert.Equal("нб", beforeSnapshot!.StatusKind.Code, StringComparer.OrdinalIgnoreCase);
    }
}
