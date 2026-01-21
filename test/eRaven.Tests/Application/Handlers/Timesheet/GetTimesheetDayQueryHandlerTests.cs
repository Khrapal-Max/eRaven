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
    private static TimesheetDayPerPersonCurrentStateDto Row(Guid personId, string main, string task)
        => new(
            PersonId: personId,
            FullName: "Ivanov Ivan",
            RNOKPP: "123",
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: EnrollmentKind.Unit,
            EnrolledAt: new DateOnly(2026, 1, 1),
            ExcludedAt: null,
            MainEntryId: null,
            MainCode: main,
            TaskEntryId: null,
            TaskCode: task);

    [Fact]
    public async Task HandleAsync_should_call_repo_with_trimmed_search_and_forward_params_and_return_result()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        var handler = new GetTimesheetDayQueryHandler(repo.Object);

        var date = new DateOnly(2026, 01, 20);

        var query = new GetTimesheetDayQuery(
            Date: date,
            Search: "  ivanov  ",
            EnrollmentKind: EnrollmentKind.Unit,
            ActiveOnly: true);

        var expected = new List<TimesheetDayPerPersonCurrentStateDto>
        {
            Row(Guid.NewGuid(), main: "30", task: "")
        };

        repo.Setup(r => r.GetDailyTimesheetAsync(
                date: date,
                search: "ivanov",
                enrollmentKind: EnrollmentKind.Unit,
                activeOnly: true,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // act
        var res = await handler.HandleAsync(query);

        // assert
        Assert.Same(expected, res);

        repo.Verify(r => r.GetDailyTimesheetAsync(
            date,
            "ivanov",
            EnrollmentKind.Unit,
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_null_search_when_whitespace()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        var handler = new GetTimesheetDayQueryHandler(repo.Object);

        var date = new DateOnly(2026, 01, 20);

        var query = new GetTimesheetDayQuery(
            Date: date,
            Search: "   ",
            EnrollmentKind: null,
            ActiveOnly: false);

        var expected = Array.Empty<TimesheetDayPerPersonCurrentStateDto>();

        repo.Setup(r => r.GetDailyTimesheetAsync(
                date: date,
                search: null,
                enrollmentKind: null,
                activeOnly: false,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // act
        var res = await handler.HandleAsync(query);

        // assert
        Assert.Same(expected, res);

        repo.Verify(r => r.GetDailyTimesheetAsync(
            date,
            null,
            null,
            false,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_forward_cancellation_token()
    {
        // arrange
        var repo = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        var handler = new GetTimesheetDayQueryHandler(repo.Object);

        var date = new DateOnly(2026, 01, 20);

        var query = new GetTimesheetDayQuery(
            Date: date,
            Search: null,
            EnrollmentKind: EnrollmentKind.AttachedByOrder,
            ActiveOnly: true);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var expected = new List<TimesheetDayPerPersonCurrentStateDto>
        {
            Row(Guid.NewGuid(), main: "НБ", task: "BT")
        };

        repo.Setup(r => r.GetDailyTimesheetAsync(
                date: date,
                search: null,
                enrollmentKind: EnrollmentKind.AttachedByOrder,
                activeOnly: true,
                ct: ct))
            .ReturnsAsync(expected);

        // act
        var res = await handler.HandleAsync(query, ct);

        // assert
        Assert.Same(expected, res);

        repo.Verify(r => r.GetDailyTimesheetAsync(
            date,
            null,
            EnrollmentKind.AttachedByOrder,
            true,
            ct), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
