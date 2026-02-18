//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// UpdatePersonalInfoCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Handlers.Personal;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class UpdatePersonalInfoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_once()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new UpdatePersonalInfoCommandHandler(repo.Object);

        var cmd = new UpdatePersonalInfoCommand(
            PersonId: Guid.NewGuid(),
            Rnokpp: "1234567890",
            LastName: "Іванов",
            FirstName: "Іван",
            MiddleName: "Іванович",
            Note: null,
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc)
        );

        repo.Setup(x => x.UpdatePersonalInfoAsync(cmd.PersonId,
            cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(x => x.UpdatePersonalInfoAsync(cmd.PersonId,
            cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new UpdatePersonalInfoCommandHandler(repo.Object);

        var cmd = new UpdatePersonalInfoCommand(
            PersonId: Guid.NewGuid(),
            Rnokpp: "1234567890",
            LastName: "Іванов",
            FirstName: "Іван",
            MiddleName: "Іванович",
            Note: null,
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc)
        );

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.UpdatePersonalInfoAsync(cmd.PersonId,
            cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc, ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(x => x.UpdatePersonalInfoAsync(cmd.PersonId,
            cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Note,
            cmd.Author,
            cmd.NowUtc,
            ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
