//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ExcludeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_validate_then_call_repo_then_close_timesheet()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: " причина ",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: " tester ",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        var seq = new MockSequence();

        ts.InSequence(seq)
            .Setup(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo.InSequence(seq)
            .Setup(r => r.ExcludeAsync(cmd.PersonId,
            cmd.Reason,
            cmd.EffectiveDate,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(x => x.CloseOnExcludeAsync(
                cmd.PersonId,
                cmd.EffectiveDate,
                cmd.Reason,
                It.Is<string>(a => a.Trim() == "tester"),
                cmd.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        await handler.HandleAsync(cmd, CancellationToken.None);

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

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var seq = new MockSequence();

        ts.InSequence(seq)
            .Setup(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, ct))
            .Returns(Task.CompletedTask);

        repo.InSequence(seq)
            .Setup(r => r.ExcludeAsync(cmd.PersonId,
            cmd.Reason,
            cmd.EffectiveDate,
            cmd.Author,
            cmd.NowUtc,
            ct))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(x => x.CloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, cmd.Reason, "tester", cmd.NowUtc, ct))
            .Returns(Task.CompletedTask);

        // act
        await handler.HandleAsync(cmd, ct);

        // assert
        repo.VerifyAll();
        ts.VerifyAll();

        repo.VerifyNoOtherCalls();
        ts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_validation_throws_should_not_call_repo_or_close()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        ts.Setup(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new InvalidOperationException("bad state"));

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));
        Assert.Equal("bad state", ex.Message);

        ts.Verify(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        // CloseOnExcludeAsync must NOT be called
        ts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_throws_should_propagate_and_not_close_timesheet()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetLifecycleRepository>(MockBehavior.Strict);

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        var seq = new MockSequence();

        ts.InSequence(seq)
            .Setup(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo.InSequence(seq)
            .Setup(r => r.ExcludeAsync(cmd.PersonId,
            cmd.Reason,
            cmd.EffectiveDate,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));
        Assert.Equal("boom", ex.Message);

        ts.Verify(x => x.ValidateCanCloseOnExcludeAsync(cmd.PersonId, cmd.EffectiveDate, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.ExcludeAsync(cmd.PersonId,
            cmd.Reason,
            cmd.EffectiveDate,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()), Times.Once);

        // Close should NOT be called
        ts.VerifyNoOtherCalls();
        repo.VerifyNoOtherCalls();
    }
}
