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
    public async Task HandleAsync_should_call_repo_with_same_params_and_return_result()
    {
        // arrange
        var repo = new Mock<ITimesheetMonthRepository>(MockBehavior.Strict);

        var query = new GetTimesheetMonthQuery(
            Year: 2026,
            Month: 1,
            Search: " ivanov ");

        var row = new TimesheetPersonMonthRowDto(
            PersonId: Guid.NewGuid(),
            FullName: "Ivanov Ivan",
            RNOKPP: "111",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            MainCodes: ["30"],
            MainRef: [null],
            TaskCodes: [""]
        );

        IReadOnlyList<TimesheetPersonMonthRowDto> expected = [row];

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
