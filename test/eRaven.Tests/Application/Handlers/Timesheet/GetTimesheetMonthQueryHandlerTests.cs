//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetMonthQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class GetTimesheetMonthQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_with_same_params_and_return_result()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthGridRepository>(MockBehavior.Strict);

        var query = new GetTimesheetMonthQuery(
            Year: 2026,
            Month: 1,
            Search: " ivanov ");

        var expected = new TimesheetMonthGridDto(
            Year: 2026,
            Month: 1,
            DaysInMonth: 31,
            Rows: []);

        repo.Setup(x => x.GetTimesheetMonthAsync(
                2026,
                1,
                " ivanov ",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetMonthQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetTimesheetMonthAsync(
            2026,
            1,
            " ivanov ",
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
