//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseMissionCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Commands.Mission;
using eRaven.Application.Handlers.Mission;
using Moq;

namespace eRaven.Tests.Application.Handlers.Mission;

public sealed class CloseMissionCommandHandlerTests
{
    [Fact(DisplayName = "CloseMissionHandler: викликає repo.CloseMissionPointAsync з MissionId/ClosedAt")]
    public async Task HandleAsync_CallsRepo_CloseMissionPointAsync()
    {
        // Arrange
        var repo = new Mock<IMissionRepository>(MockBehavior.Strict);

        repo.Setup(r => r.CloseMissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CloseMissionCommandHandler(repo.Object);

        var cmd = new CloseMissionCommand(
            MissionId: Guid.NewGuid(),
            ClosedAt: new DateOnly(2026, 01, 10)
        );

        // Act
        await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CloseMissionAsync(
            cmd.MissionId,
            cmd.ClosedAt,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "CloseMissionHandler: передає CancellationToken в репозиторій")]
    public async Task HandleAsync_PassesCancellationToken()
    {
        // Arrange
        var repo = new Mock<IMissionRepository>(MockBehavior.Strict);

        CancellationToken captured = default;

        repo.Setup(r => r.CloseMissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, DateOnly, CancellationToken>((_, _, ct) => captured = ct)
            .Returns(Task.CompletedTask);

        var handler = new CloseMissionCommandHandler(repo.Object);

        var cmd = new CloseMissionCommand(
            MissionId: Guid.NewGuid(),
            ClosedAt: new DateOnly(2026, 01, 10)
        );

        using var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(cmd, cts.Token);

        // Assert
        Assert.Equal(cts.Token, captured);

        repo.Verify(r => r.CloseMissionAsync(
            cmd.MissionId,
            cmd.ClosedAt,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
