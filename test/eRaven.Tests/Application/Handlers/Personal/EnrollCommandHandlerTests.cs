//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class EnrollCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsPersonEnrollThenOpensTimesheetEpisode()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetEpisodeRepository>(MockBehavior.Strict);

        var nowUtc = new DateTime(2026, 02, 25, 10, 30, 00, DateTimeKind.Utc);
        var enrollDate = new DateOnly(2026, 02, 20);
        var personId = Guid.NewGuid();

        var command = new EnrollCommand(
            PersonId: personId,
            Kind: default,
            Reference: "REF-1",
            Reason: "Enroll",
            EnrollDate: enrollDate,
            Rank: "Sgt",
            PositionSort: 10,
            Position: "Operator",
            Author: "tester",
            NowUtc: nowUtc);

        var seq = new MockSequence();

        repo.InSequence(seq)
            .Setup(r => r.EnrollAsync(
                command.PersonId,
                EnrollmentKind.Unit,
                command.Reference,
                command.Reason,
                command.EnrollDate,
                command.Rank,
                command.PositionSort,
                command.Position,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ts.InSequence(seq)
            .Setup(r => r.OpenOnEnrollAsync(
                command.PersonId,
                command.EnrollDate,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new EnrollCommandHandler(repo.Object, ts.Object);

        // act
        await handler.HandleAsync(command);

        // assert
        repo.VerifyAll();
        ts.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPersonEnrollFails_DoesNotOpenTimesheetEpisode()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var ts = new Mock<ITimesheetEpisodeRepository>(MockBehavior.Strict);

        var nowUtc = new DateTime(2026, 02, 25, 10, 30, 00, DateTimeKind.Utc);
        var enrollDate = new DateOnly(2026, 02, 20);
        var personId = Guid.NewGuid();

        var command = new EnrollCommand(
            PersonId: personId,
            Kind: default,
            Reference: null,
            Reason: "Enroll",
            EnrollDate: enrollDate,
            Rank: "Sgt",
            PositionSort: 10,
            Position: "Operator",
            Author: "tester",
            NowUtc: nowUtc);

        repo.Setup(r => r.EnrollAsync(
                command.PersonId,
                EnrollmentKind.Unit,
                command.Reference,
                command.Reason,
                command.EnrollDate,
                command.Rank,
                command.PositionSort,
                command.Position,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        var handler = new EnrollCommandHandler(repo.Object, ts.Object);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));

        // assert
        Assert.Equal("fail", ex.Message);
        ts.Verify(
            x => x.OpenOnEnrollAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
