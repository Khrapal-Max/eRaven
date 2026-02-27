//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ExcludeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidatesThenExcludesThenClosesTimesheetEpisode()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetEpisodeRepository>(MockBehavior.Strict);

        var nowUtc = new DateTime(2026, 02, 25, 10, 30, 00, DateTimeKind.Utc);
        var effective = new DateOnly(2026, 02, 25);
        var personId = Guid.NewGuid();

        var command = new ExcludeCommand(
            PersonId: personId,
            Reason: "Exclude",
            EffectiveDate: effective,
            Author: "tester",
            NowUtc: nowUtc);

        var seq = new MockSequence();

        ts.InSequence(seq)
            .Setup(x => x.ValidateCanCloseOnExcludeAsync(
                command.PersonId,
                command.EffectiveDate,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo.InSequence(seq)
            .Setup(x => x.ExcludeAsync(
                command.PersonId,
                command.Reason,
                command.EffectiveDate,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(x => x.CloseOnExcludeAsync(
                command.PersonId,
                command.EffectiveDate,
                command.Reason,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        // act
        await handler.HandleAsync(command);

        // assert
        repo.VerifyAll();
        ts.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_DoesNotExcludeAndDoesNotCloseEpisode()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetEpisodeRepository>(MockBehavior.Strict);

        var nowUtc = new DateTime(2026, 02, 25, 10, 30, 00, DateTimeKind.Utc);
        var effective = new DateOnly(2026, 02, 25);
        var personId = Guid.NewGuid();

        var command = new ExcludeCommand(
            PersonId: personId,
            Reason: "Exclude",
            EffectiveDate: effective,
            Author: "tester",
            NowUtc: nowUtc);

        ts.Setup(x => x.ValidateCanCloseOnExcludeAsync(
                command.PersonId,
                command.EffectiveDate,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not allowed"));

        var handler = new ExcludeCommandHandler(repo.Object, ts.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));

        // assert
        Assert.Equal("not allowed", ex.Message);

        repo.Verify(
            x => x.ExcludeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);

        ts.Verify(
            x => x.CloseOnExcludeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
