//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetsMonthQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoPeriods_ReturnsEmpty_AndDoesNotLoadPersons()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: null);

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        Assert.Empty(result);

        persons.Verify(
            x => x.GetByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()),
            Times.Never);

        repo.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_MapsPersonAndDays_FromReadModels()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: null);

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePeriod(personId, query.Year, query.Month, code: "Т")]);

        persons.Setup(r => r.GetByIdsAsync(
                It.Is<Guid[]>(ids => ids.Length == 1 && ids[0] == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreatePersonCard(personId, fullName: "Alpha", positionSort: 1),
            ]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        var row = Assert.Single(result);
        Assert.Equal(personId, row.Person.PersonId);

        var daysInMonth = DateTime.DaysInMonth(query.Year, query.Month);
        Assert.Equal(daysInMonth, row.Days.Count);

        Assert.Equal(new DateOnly(query.Year, query.Month, 1), row.Days[0].Date);
        Assert.Equal("Т", row.Days[0].Code);
        Assert.False(row.Days[0].IsDerived);
        Assert.True(row.Days[0].IsChangePoint);
        Assert.Equal(TimesheetUiStyleDto.Ready, row.Days[0].UiStyle);

        repo.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationTokenToDependencies()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: null);
        var ct = new CancellationTokenSource().Token;

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.Is<CancellationToken>(x => x == ct)))
            .ReturnsAsync([CreatePeriod(personId, 2026, 2, "Т")]);

        persons.Setup(r => r.GetByIdsAsync(
                It.Is<Guid[]>(ids => ids.Length == 1 && ids[0] == personId),
                It.Is<CancellationToken>(x => x == ct)))
            .ReturnsAsync([CreatePersonCard(personId, "Alpha", 1)]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query, ct);

        // assert
        Assert.Single(result);
        repo.VerifyAll();
        persons.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_LoadsDistinctPersonIds_AndSkipsRowsWithoutPersonCards()
    {
        // arrange
        var repo = new Mock<ITimesheetViewRepository>(MockBehavior.Strict);
        var persons = new Mock<IPersonRepository>(MockBehavior.Strict);

        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(personA, 2026, 2, "Т"),
            CreatePeriod(personB, 2026, 2, "Т"),
        };

        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: null);

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        // Return only A -> row for B must be skipped.
        persons.Setup(r => r.GetByIdsAsync(
                It.Is<Guid[]>(ids => ids.Length == 2 && ids.Contains(personA) && ids.Contains(personB)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePersonCard(personA, "Alpha", 1)]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        var row = Assert.Single(result);
        Assert.Equal(personA, row.Person.PersonId);

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

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(personA, 2026, 2, "Т"),
            CreatePeriod(personB, 2026, 2, "Т"),
        };

        // search must be trimmed inside handler
        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: "  Alpha  ");

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        persons.Setup(r => r.GetByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreatePersonCard(personA, fullName: "Alpha", positionSort: 2),
                CreatePersonCard(personB, fullName: "Beta", positionSort: 1),
            ]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        var row = Assert.Single(result);
        Assert.Equal(personA, row.Person.PersonId);
        Assert.Equal("Alpha", row.Person.FullName);

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

        var periods = new List<TimesheetPeriodRm>
        {
            CreatePeriod(p1, 2026, 2, "Т"),
            CreatePeriod(p2, 2026, 2, "Т"),
            CreatePeriod(p3, 2026, 2, "Т"),
        };

        var query = new GetTimesheetsMonthQuery(Year: 2026, Month: 2, Search: null);

        repo.Setup(r => r.GetTimesheetsMonthAsync(query.Year, query.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periods);

        // Expected order: PositionSort asc, then FullName asc.
        persons.Setup(r => r.GetByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreatePersonCard(p1, fullName: "Charlie", positionSort: 10),
                CreatePersonCard(p2, fullName: "Alpha", positionSort: 5),
                CreatePersonCard(p3, fullName: "Bravo", positionSort: null),
            ]);

        var handler = new GetTimesheetsMonthQueryHandler(repo.Object, persons.Object);

        // act
        var result = await handler.HandleAsync(query);

        // assert
        Assert.Equal(3, result.Count);
        Assert.Equal(p2, result[0].Person.PersonId); // sort=5
        Assert.Equal(p1, result[1].Person.PersonId); // sort=10
        Assert.Equal(p3, result[2].Person.PersonId); // sort=null -> MaxValue

        repo.VerifyAll();
        persons.VerifyAll();
    }

    //======================================================================
    // Test data builders
    //======================================================================

    private static TimesheetPeriodRm CreatePeriod(Guid personId, int year, int month, string code)
    {
        var days = DateTime.DaysInMonth(year, month);
        var timesheetId = Guid.NewGuid();
        var list = new List<TimesheetDayRm>(days);

        for (var d = 1; d <= days; d++)
        {
            list.Add(new TimesheetDayRm(
                TimesheetId: timesheetId,
                DateOfDay: new DateOnly(year, month, d),
                CodeId: Guid.NewGuid(),
                Code: code,
                Reference: null,
                Note: null,
                IsDerived: false,
                IsChangePoint: d == 1,
                UiStyle: TimesheetUiStyle.Ready));
        }

        return new TimesheetPeriodRm(personId, list);
    }

    private static PersonReadModel CreatePersonCard(Guid id, string fullName, int? positionSort)
        => new()
        {
            Id = id,
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit,
            EnrollmentReference = string.Empty,
            Rnokpp = "1234567891",
            LastName = "LastName",
            FirstName = "FirstName",
            MiddleName = "MiddleName",
            FullName = fullName,
            Rank = "soldier",
            PositionSort = positionSort,
            Position = "commander",
            Bzvp = "bzvp",
            Weapon = "Weapon"
        };
}
