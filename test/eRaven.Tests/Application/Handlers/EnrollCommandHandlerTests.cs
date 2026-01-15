//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public sealed class EnrollCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_id()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new EnrollCommandHandler(repo.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: eRaven.Domain.Enums.EnrollmentKind.Unit,
            Reference: "A",
            Reason: "r",
            EnrollDate: new DateOnly(2026, 01, 10),
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        var expectedId = cmd.PersonId;

        repo.Setup(x => x.EnrollAsync(cmd, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(x => x.EnrollAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new EnrollCommandHandler(repo.Object);

        var cmd = new EnrollCommand(
            PersonId: Guid.NewGuid(),
            Kind: eRaven.Domain.Enums.EnrollmentKind.Unit,
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

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(x => x.EnrollAsync(cmd, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}