//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetsRangeQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetsRangeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoPeriods_ReturnsEmpty_AndDoesNotLoadPersons()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var query = new GetTimesheetsRangeQuery(
            From: new DateOnly(2026, 2, 10),
            To: new DateOnly(2026, 2, 16),
            Search: null);

        repo.Setup(r => r.GetTimesheetsRangeAsync(query.From, query.To, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetTimesheetsRangeQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        Assert.Empty(result);
        repo.VerifyAll();
        persons.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_LoadsDistinctPersonIds_AndSkipsRowsWithoutPersonCards()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 10);
        var to = new DateOnly(2026, 2, 16);

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(personA, from, to, "Т"),
            CreatePeriod(personB, from, to, "Т"),
        };

        var query = new GetTimesheetsRangeQuery(from, to, Search: null);

        repo.Setup(r => r.GetTimesheetsRangeAsync(query.From, query.To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        // Return only A -> row for B must be skipped.
        persons.Setup(r => r.GetByIdsAsync(
                It.Is<Guid[]>(ids => ids.Length == 2 && ids.Contains(personA) && ids.Contains(personB)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePersonCard(personA, "Alpha", 1)]);

        var handler = new GetTimesheetsRangeQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        var row = Assert.Single(result);
        Assert.Equal(personA, row.Person.PersonId);
        Assert.Equal(to.DayNumber - from.DayNumber + 1, row.Days.Count);

        repo.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_AppliesSearchTrim_AndFiltersPersons()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 10);
        var to = new DateOnly(2026, 2, 16);

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(personA, from, to, "Т"),
            CreatePeriod(personB, from, to, "Т"),
        };

        var query = new GetTimesheetsRangeQuery(from, to, Search: "  Alpha  ");

        repo.Setup(r => r.GetTimesheetsRangeAsync(query.From, query.To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        persons.Setup(r => r.GetByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePersonCard(personA, "Alpha", 1),
                CreatePersonCard(personB, "Bravo", 1),
            ]);

        var handler = new GetTimesheetsRangeQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        var row = Assert.Single(result);
        Assert.Equal(personA, row.Person.PersonId);

        repo.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_SortsByPositionSortThenFullName()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 10);
        var to = new DateOnly(2026, 2, 16);

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(p1, from, to, "Т"),
            CreatePeriod(p2, from, to, "Т"),
            CreatePeriod(p3, from, to, "Т"),
        };

        var query = new GetTimesheetsRangeQuery(from, to, Search: null);

        repo.Setup(r => r.GetTimesheetsRangeAsync(query.From, query.To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        // PositionSort: 2 ("Alpha"), 2 ("Bravo"), null ("Charlie") => Alpha, Bravo, Charlie
        persons.Setup(r => r.GetByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreatePersonCard(p1, "Charlie", null),
                CreatePersonCard(p2, "Bravo", 2),
                CreatePersonCard(p3, "Alpha", 2),
            ]);

        var handler = new GetTimesheetsRangeQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        Assert.Equal(3, result.Count);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], [.. result.Select(r => r.Person.FullName)]);

        repo.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationTokenToDependencies()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personA = Guid.NewGuid();
        var from = new DateOnly(2026, 2, 10);
        var to = new DateOnly(2026, 2, 16);

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(personA, from, to, "Т"),
        };

        var query = new GetTimesheetsRangeQuery(from, to, Search: null);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(r => r.GetTimesheetsRangeAsync(query.From, query.To, It.Is<CancellationToken>(x => x == ct)))
            .ReturnsAsync(periods);

        persons.Setup(r => r.GetByIdsAsync(It.IsAny<Guid[]>(), It.Is<CancellationToken>(x => x == ct)))
            .ReturnsAsync([CreatePersonCard(personA, "Alpha", 1)]);

        var handler = new GetTimesheetsRangeQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query, ct);

        // assert
        Assert.Single(result);
        repo.VerifyAll();
        persons.VerifyAll();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static TimesheetPeriodRm CreatePeriod(Guid personId, DateOnly from, DateOnly to, string code)
    {
        var count = to.DayNumber - from.DayNumber + 1;
        var timesheetId = Guid.NewGuid();
        var days = new List<TimesheetDayRm>(count);

        for (var i = 0; i < count; i++)
        {
            var date = from.AddDays(i);

            days.Add(new TimesheetDayRm(
                TimesheetId: timesheetId,
                DateOfDay: date,
                CodeId: Guid.NewGuid(),
                Code: code,
                Reference: null,
                Note: null,
                IsDerived: false,
                IsChangePoint: i == 0,
                UiStyle: TimesheetUiStyle.Ready));
        }

        return new TimesheetPeriodRm(personId, days);
    }

    private static PersonReadModel CreatePersonCard(Guid id, string fullName, int? positionSort)
        => new()
        {
            Id = id,
            FullName = fullName,
            PositionSort = positionSort,
        };
}
