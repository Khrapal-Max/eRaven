//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class GetTimesheetPersonMonthQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_validate_range_call_repo_and_return_dto()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        var person = new TimesheetPersonMonthRowDto(
            PersonId: personId,
            FullName: "Ivanov Ivan",
            RNOKPP: "111",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            MainCodes: [.. Enumerable.Repeat("30", 31)],
            MainRef: [.. Enumerable.Repeat<string?>(null, 31)],
            TaskCodes: [.. Enumerable.Repeat("", 31)]
        );

        var expected = new TimesheetPersonMonthDto(
            Person: person,
            Year: year,
            Month: month,
            DaysInMonth: 31,
            UpdatedAtUtc: new DateTime(2026, 01, 23, 12, 0, 0, DateTimeKind.Utc),
            Entries: []
        );

        repo.Setup(x => x.GetTimesheetPersonMonthAsync(
                personId, year, month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetPersonMonthQueryHandler(repo.Object);

        var query = new GetTimesheetPersonMonthQuery(
            PersonId: personId,
            Year: year,
            Month: month);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetTimesheetPersonMonthAsync(
            personId, year, month, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_person_not_found_should_throw_invalid_operation()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var personId = Guid.NewGuid();
        var year = 2026;
        var month = 1;

        repo.Setup(x => x.GetTimesheetPersonMonthAsync(
                personId, year, month, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimesheetPersonMonthDto?)null);

        var sut = new GetTimesheetPersonMonthQueryHandler(repo.Object);

        var query = new GetTimesheetPersonMonthQuery(
            PersonId: personId,
            Year: year,
            Month: month);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(query, CancellationToken.None));

        // assert
        Assert.Contains("Person not found", ex.Message, StringComparison.OrdinalIgnoreCase);

        repo.Verify(x => x.GetTimesheetPersonMonthAsync(
            personId, year, month, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2101, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task HandleAsync_when_year_or_month_out_of_range_should_throw_and_not_call_repo(int year, int month)
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);
        var sut = new GetTimesheetPersonMonthQueryHandler(repo.Object);

        var query = new GetTimesheetPersonMonthQuery(
            PersonId: Guid.NewGuid(),
            Year: year,
            Month: month);

        // act + assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.HandleAsync(query, CancellationToken.None));

        repo.VerifyNoOtherCalls();
    }
}
