//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Commands.Mission;
using eRaven.Application.Handlers.Mission;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Mission;

public sealed class CreateMissionCommandHandlerTests
{
    [Fact(DisplayName = "CreateMissionHandler: викликає repo.AddMissionPoint з тими ж параметрами і повертає Guid")]
    public async Task HandleAsync_CallsRepo_AndReturnsId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        var repo = new Mock<IMissionRepository>(MockBehavior.Strict);

        repo.Setup(r => r.AddMission(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<MissionMode>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var handler = new CreateMissionCommandHandler(repo.Object);

        var cmd = new CreateMissionCommand(
            PositionArea: "Район-1",
            NamePoint: "",
            DroneName: "DJI",
            Target: "Розвідка",
            MissionMode: MissionMode.Day,
            TodayLocal: new DateTime(2026, 01, 10, 9, 0, 0, DateTimeKind.Local)
        );

        // Act
        var id = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        Assert.Equal(expectedId, id);

        repo.Verify(r => r.AddMission(
            positionArea: cmd.PositionArea,
            namePoint: cmd.NamePoint,
            typeDrone: cmd.DroneName,
            target: cmd.Target,
            missionMode: cmd.MissionMode,
            todayLocal: cmd.TodayLocal,
            ct: It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "CreateMissionHandler: передає CancellationToken в репозиторій")]
    public async Task HandleAsync_PassesCancellationToken()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        var repo = new Mock<IMissionRepository>(MockBehavior.Strict);

        CancellationToken captured = default;

        repo.Setup(r => r.AddMission(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<MissionMode>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string?, string?, string, MissionMode, DateTime, CancellationToken>((_, _, _, _, _, _, ct) => captured = ct)
            .ReturnsAsync(expectedId);

        var handler = new CreateMissionCommandHandler(repo.Object);

        var cmd = new CreateMissionCommand(
            PositionArea: "A",
            NamePoint: "",
            DroneName: null,
            Target: "T",
            MissionMode: MissionMode.Night,
            TodayLocal: new DateTime(2026, 01, 10)
        );

        using var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(cmd, cts.Token);

        // Assert
        Assert.Equal(cts.Token, captured);

        repo.Verify(r => r.AddMission(
            cmd.PositionArea,
            cmd.NamePoint,
            cmd.DroneName,
            cmd.Target,
            cmd.MissionMode,
            cmd.TodayLocal,
            It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
