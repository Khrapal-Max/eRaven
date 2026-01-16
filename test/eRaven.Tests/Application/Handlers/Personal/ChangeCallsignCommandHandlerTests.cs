//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsignCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ChangeCallsignCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_once()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new ChangeCallsignCommandHandler(repo.Object);

        var cmd = new ChangeCallsignCommand(
            PersonId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 01, 10),
            Callsign: "Дніпро",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc));

        repo.Setup(x => x.ChangeCallsignAsync(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(x => x.ChangeCallsignAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new ChangeCallsignCommandHandler(repo.Object);

        var cmd = new ChangeCallsignCommand(
            PersonId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 01, 10),
            Callsign: null,
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.ChangeCallsignAsync(cmd, ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(x => x.ChangeCallsignAsync(cmd, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
