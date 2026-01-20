//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class EnrollCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_person_repo_then_timesheet_ensure_opened()
    {
        // arrange
        var calls = new List<string>();

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        repo.Setup(x => x.EnrollAsync(It.IsAny<EnrollCommand>(), It.IsAny<CancellationToken>()))
            .Returns((EnrollCommand _, CancellationToken __) =>
            {
                calls.Add("person");
                return Task.CompletedTask;
            });

        var timesheet = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        timesheet.Setup(x => x.EnsureOpenedOnEnrollAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns((Guid _, DateOnly __, string ___, DateTime ____, CancellationToken _____) =>
            {
                calls.Add("timesheet");
                return Task.CompletedTask;
            });

        var sut = new EnrollCommandHandler(repo.Object, timesheet.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(x => x.EnrollAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);

        timesheet.Verify(x => x.EnsureOpenedOnEnrollAsync(
            personId: cmd.PersonId,
            enrollDate: cmd.EnrollDate,
            author: cmd.Author,
            nowUtc: cmd.NowUtc,
            ct: It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(new[] { "person", "timesheet" }, calls);

        repo.VerifyNoOtherCalls();
        timesheet.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_both_calls()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var timesheet = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var sut = new EnrollCommandHandler(repo.Object, timesheet.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: EnrollmentKind.Unit,
            Reference: null,
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.EnrollAsync(cmd, ct)).Returns(Task.CompletedTask);

        timesheet.Setup(x => x.EnsureOpenedOnEnrollAsync(
                cmd.PersonId,
                cmd.EnrollDate,
                cmd.Author,
                cmd.NowUtc,
                ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(x => x.EnrollAsync(cmd, ct), Times.Once);

        timesheet.Verify(x => x.EnsureOpenedOnEnrollAsync(
            cmd.PersonId,
            cmd.EnrollDate,
            cmd.Author,
            cmd.NowUtc,
            ct), Times.Once);

        repo.VerifyNoOtherCalls();
        timesheet.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_not_call_timesheet_if_person_repo_fails()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var timesheet = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var sut = new EnrollCommandHandler(repo.Object, timesheet.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        repo.Setup(x => x.EnrollAsync(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // act + assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(cmd));

        timesheet.Verify(
            x => x.EnsureOpenedOnEnrollAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repo.Verify(x => x.EnrollAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        timesheet.VerifyNoOtherCalls();
    }
}
