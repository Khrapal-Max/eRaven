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
    public async Task HandleAsync_should_call_repo_then_open_timesheet()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var sut = new EnrollCommandHandler(repo.Object, ts.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Author: " tester ",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        var seq = new MockSequence();

        repo.InSequence(seq)
            .Setup(x => x.EnrollAsync(cmd.PersonId,
            cmd.Kind,
            cmd.Reference,
            cmd.Reason,
            cmd.EnrollDate,
            cmd.Rank,
            cmd.PositionSort,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(x => x.OpenOnEnrollAsync(
                cmd.PersonId,
                cmd.EnrollDate,
                It.Is<string>(a => a.Trim() == "tester"),
                cmd.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.VerifyAll();
        ts.VerifyAll();

        repo.VerifyNoOtherCalls();
        ts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_dependencies()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var sut = new EnrollCommandHandler(repo.Object, ts.Object);

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

        var seq = new MockSequence();

        repo.InSequence(seq)
            .Setup(x => x.EnrollAsync(cmd.PersonId,
            cmd.Kind,
            cmd.Reference,
            cmd.Reason,
            cmd.EnrollDate,
            cmd.Rank,
            cmd.PositionSort,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            ct))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(x => x.OpenOnEnrollAsync(cmd.PersonId, cmd.EnrollDate, "tester", cmd.NowUtc, ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.VerifyAll();
        ts.VerifyAll();

        repo.VerifyNoOtherCalls();
        ts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_throws_should_propagate_and_not_touch_timesheet()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var sut = new EnrollCommandHandler(repo.Object, ts.Object);

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

        repo.Setup(x => x.EnrollAsync(cmd.PersonId,
            cmd.Kind,
            cmd.Reference,
            cmd.Reason,
            cmd.EnrollDate,
            cmd.Rank,
            cmd.PositionSort,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(cmd));
        Assert.Equal("boom", ex.Message);

        repo.Verify(x => x.EnrollAsync(cmd.PersonId,
            cmd.Kind,
            cmd.Reference,
            cmd.Reason,
            cmd.EnrollDate,
            cmd.Rank,
            cmd.PositionSort,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
        ts.VerifyNoOtherCalls();
    }
}
