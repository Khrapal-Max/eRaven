//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonMove;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ExcludeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_id()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        var cmd = new ExcludeCommand(
            PersonId: id,
            Reason: " причина ",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        repo.Setup(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new ExcludeCommandHandler(repo.Object);

        // act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // assert
        repo.Verify(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        var cmd = new ExcludeCommand(
            PersonId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(r => r.ExcludeAsync(cmd, ct)).Returns(Task.CompletedTask);

        var handler = new ExcludeCommandHandler(repo.Object);

        // act
        await handler.HandleAsync(cmd, ct);

        // assert
        repo.Verify(r => r.ExcludeAsync(cmd, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_throws_should_propagate_exception()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var id = Guid.NewGuid();
        var cmd = new ExcludeCommand(
            PersonId: id,
            Reason: "x",
            EffectiveDate: new DateOnly(2026, 01, 20),
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

        repo.Setup(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new ExcludeCommandHandler(repo.Object);

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Equal("boom", ex.Message);
        repo.Verify(r => r.ExcludeAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
