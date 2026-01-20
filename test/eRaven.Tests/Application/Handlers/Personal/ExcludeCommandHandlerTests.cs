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
    private static readonly string[] expected = ["person", "timesheet"];

    [Fact]
    public async Task HandleAsync_should_call_person_repo_then_timesheet_close()
    {
        // arrange
        var calls = new List<string>();

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ExcludeAsync(It.IsAny<ExcludeCommand>(), It.IsAny<CancellationToken>()))
            .Returns((ExcludeCommand _, CancellationToken __) =>
            {
                calls.Add("person");
                return Task.CompletedTask;
            });

        var timesheet = new Mock<ITimesheetRepository>(MockBehavior.Strict);
        timesheet.Setup(t => t.EnsureClosedOnExcludeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns((Guid _, DateOnly __, string? ___, string ____, DateTime _____, CancellationToken ______) =>
            {
                calls.Add("timesheet");
                return Task.CompletedTask;
            });

        var sut = new ExcludeCommandHandler(repo.Object, timesheet.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: " причина ",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);

        timesheet.Verify(t => t.EnsureClosedOnExcludeAsync(
            personId: cmd.PersonId,
            closeTo: cmd.EffectiveDate,
            reason: cmd.Reason,
            author: cmd.Author,
            nowUtc: cmd.NowUtc,
            ct: It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(expected, calls);

        repo.VerifyNoOtherCalls();
        timesheet.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_both_calls()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var timesheet = new Mock<ITimesheetRepository>(MockBehavior.Strict);

        var sut = new ExcludeCommandHandler(repo.Object, timesheet.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(r => r.ExcludeAsync(cmd, ct)).Returns(Task.CompletedTask);

        timesheet.Setup(t => t.EnsureClosedOnExcludeAsync(
                cmd.PersonId,
                cmd.EffectiveDate,
                cmd.Reason,
                cmd.Author,
                cmd.NowUtc,
                ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(r => r.ExcludeAsync(cmd, ct), Times.Once);

        timesheet.Verify(t => t.EnsureClosedOnExcludeAsync(
            cmd.PersonId,
            cmd.EffectiveDate,
            cmd.Reason,
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

        var sut = new ExcludeCommandHandler(repo.Object, timesheet.Object);

        var cmd = new ExcludeCommand(
            PersonId: Guid.NewGuid(),
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        repo.Setup(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // act + assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(cmd));

        timesheet.Verify(t => t.EnsureClosedOnExcludeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repo.Verify(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        timesheet.VerifyNoOtherCalls();
    }
}
