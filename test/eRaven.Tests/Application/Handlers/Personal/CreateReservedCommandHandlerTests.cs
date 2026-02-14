//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class CreateReservedCommandHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    private static CreateReservedCommand Cmd() => new(
        Rnokpp: "1234567890",
        LastName: "Ivanov",
        FirstName: "Ivan",
        MiddleName: "Ivanovich",
        Rank: "солдат",
        Position: "стрілець",
        Author: "tester",
        NowUtc: NowUtc
    );

    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_id()
    {
        // arrange
        var cmd = Cmd();
        var expectedId = Guid.NewGuid();

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        repo.Setup(x => x.CreateReservedAsync(cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Rank,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var sut = new CreateReservedCommandHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(cmd, CancellationToken.None);

        // assert
        Assert.Equal(expectedId, result);

        repo.Verify(x => x.CreateReservedAsync(cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Rank,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_throws_should_propagate_exception()
    {
        // arrange
        var cmd = Cmd();
        var repoEx = new InvalidOperationException("boom");

        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        repo.Setup(x => x.CreateReservedAsync(cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Rank,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(repoEx);

        var sut = new CreateReservedCommandHandler(repo.Object);

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(cmd, CancellationToken.None));

        Assert.Equal("boom", ex.Message);

        repo.Verify(x => x.CreateReservedAsync(cmd.Rnokpp,
            cmd.LastName,
            cmd.FirstName,
            cmd.MiddleName,
            cmd.Rank,
            cmd.Position,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
