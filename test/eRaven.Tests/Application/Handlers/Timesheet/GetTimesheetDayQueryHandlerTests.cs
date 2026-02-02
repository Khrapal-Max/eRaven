//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetDayQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries.Timesheet;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheet;

public sealed class GetTimesheetDayQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_trim_search_and_call_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var date = new DateOnly(2026, 01, 12);
        var expected = new List<TimesheetPersonDayRowDto>
        {
            new(
                PersonId: Guid.NewGuid(),
                FullName: "Ivanov Ivan",
                RNOKPP: "111",
                Rank: null,
                Position: null,
                EnrollmentKind: EnrollmentKind.Unit,
                EnrolledAt: new DateOnly(2026, 01, 01),
                ExcludedAt: null,
                DayState: new TimesheetDayStateDto("30", "REF", "NOTE")
            )
        };

        repo.Setup(x => x.GetTimesheetDayAsync(
                date,
                "ivanov",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetDayQueryHandler(repo.Object);

        var query = new GetTimesheetDayQuery(
            Date: date,
            Search: "  ivanov  ");

        // act
        var rows = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Same(expected, rows);

        repo.Verify(x => x.GetTimesheetDayAsync(date, "ivanov", It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_search_is_whitespace_should_pass_null_to_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var date = new DateOnly(2026, 01, 12);
        IReadOnlyList<TimesheetPersonDayRowDto> expected = [];

        repo.Setup(x => x.GetTimesheetDayAsync(
                date,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetTimesheetDayQueryHandler(repo.Object);

        // act
        var rows = await sut.HandleAsync(new GetTimesheetDayQuery(date, "   "), CancellationToken.None);

        // assert
        Assert.Same(expected, rows);

        repo.Verify(x => x.GetTimesheetDayAsync(date, null, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
