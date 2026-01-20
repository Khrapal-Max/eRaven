//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class GetTimesheetMonthQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_with_year_month_search_and_return_result()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var query = new GetTimesheetMonthQuery(
            Year: 2026,
            Month: 1,
            Search: "  ivan  ");

        var expected = new List<TimesheetMonthPerPersonDto>
        {
            new(
                PersonId: Guid.NewGuid(),
                FullName: "Ivanov Ivan",
                RNOKPP: "1234567890",
                Rank: "Солдат",
                Position: "Стрілець",
                EnrollmentKind: EnrollmentKind.Unit,
                EnrolledAt: new DateOnly(2026, 01, 01),
                ExcludedAt: null,
                Timesheet: null)
        };

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                year: query.Year,
                month: query.Month,
                search: query.Search,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetMonthQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetMonthlyTimesheetAsync(
            query.Year,
            query.Month,
            query.Search,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var query = new GetTimesheetMonthQuery(
            Year: 2026,
            Month: 2,
            Search: null);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var expected = Array.Empty<TimesheetMonthPerPersonDto>();

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                year: query.Year,
                month: query.Month,
                search: query.Search,
                ct: ct))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetMonthQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetMonthlyTimesheetAsync(
            query.Year,
            query.Month,
            query.Search,
            ct), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_throws_should_propagate_exception()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var query = new GetTimesheetMonthQuery(
            Year: 2026,
            Month: 3,
            Search: "x");

        repo.Setup(x => x.GetMonthlyTimesheetAsync(
                year: query.Year,
                month: query.Month,
                search: query.Search,
                ct: It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new GetTimesheetMonthQueryHandler(repo.Object);

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(query, CancellationToken.None));

        Assert.Equal("boom", ex.Message);

        repo.Verify(x => x.GetMonthlyTimesheetAsync(
            query.Year,
            query.Month,
            query.Search,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
