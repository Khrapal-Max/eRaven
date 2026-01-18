//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeWeaponCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.Handlers.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class ChangeWeaponCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_once()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new ChangeWeaponCommandHandler(repo.Object);

        var cmd = new ChangeWeaponCommand(
            PersonId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 01, 10),
            Weapon: "АК-74",
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc));

        repo.Setup(x => x.ChangeWeaponAsync(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd);

        // assert
        repo.Verify(x => x.ChangeWeaponAsync(cmd, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new ChangeWeaponCommandHandler(repo.Object);

        var cmd = new ChangeWeaponCommand(
            PersonId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 01, 10),
            Weapon: null,
            Author: "tester",
            NowUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc));

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.ChangeWeaponAsync(cmd, ct))
            .Returns(Task.CompletedTask);

        // act
        await sut.HandleAsync(cmd, ct);

        // assert
        repo.Verify(x => x.ChangeWeaponAsync(cmd, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
