//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocumentHandlersTests
//-----------------------------------------------------------------------------

using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Handlers.CombatTask;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTask;

public sealed class CombatTaskPlanDocumentHandlersTests
{
    [Fact]
    public async Task CreateHandler_CallsRepo_CreateDraftAsync_AndReturnsDocumentId()
    {
        // Arrange
        var repo = new Mock<ICombatTaskPlanDocumentRepository>(MockBehavior.Strict);

        var expectedId = Guid.NewGuid();

        repo.Setup(r => r.CreateDraftAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Якщо хендлер тепер повертає Guid, то він має бути ICommandHandler<..., Guid>
        var h = new CreateCombatTaskPlanDocumentCommandHandler(repo.Object);

        var personId = Guid.NewGuid();

        var line = new CombatTaskPlanLineInputDto(
            Kind: CombatTaskPlanLineKind.Start,
            PersonId: personId,
            ActionDate: new DateOnly(2026, 1, 1),
            RNOKPP: "123",
            FullName: "Test",
            Rank: null,
            Position: null,
            Weapon: null,
            Callsign: null,
            PositionalArea: "A",
            GroupName: "G",
            AssetType: null,
            Mode: CombatTaskMode.Day,
            Goal: "Goal",
            IsActual: true,
            Note: null,
            AssignmentId: null
        );

        var cmd = new CreateCombatTaskPlanDraftDocumentCommand(
            DocumentId: Guid.NewGuid(),
            RecordedAt: new DateOnly(2026, 1, 1),
            PlanningDate: new DateOnly(2026, 1, 1),
            PlanningDocTitle: "План №1",
            Author: "ui",
            NowUtc: DateTime.UtcNow);

        // Act
        var docId = await h.HandleAsync(cmd);

        // Assert (return value)
        Assert.Equal(expectedId, docId);

        // Assert (call)
        repo.Verify(r => r.CreateDraftAsync(
            cmd.RecordedAt,
            cmd.PlanningDate,
            cmd.PlanningDocTitle,
            cmd.Author,
            cmd.NowUtc,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostHandler_CallsRepo_PostAsync()
    {
        // Arrange
        var repo = new Mock<ICombatTaskPlanDocumentRepository>(MockBehavior.Strict);

        repo.Setup(r => r.PostAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var h = new PostCombatTaskPlanDocumentCommandHandler(repo.Object);

        var cmd = new PostCombatTaskPlanDocumentCommand(
            DocumentId: Guid.NewGuid(),
            Author: "ui",
            NowUtc: DateTime.UtcNow);

        // Act
        await h.HandleAsync(cmd);

        // Assert
        repo.Verify(r => r.PostAsync(cmd.DocumentId, cmd.Author, cmd.NowUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelHandler_CallsRepo_CancelAsync()
    {
        // Arrange
        var repo = new Mock<ICombatTaskPlanDocumentRepository>(MockBehavior.Strict);

        repo.Setup(r => r.CancelAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var h = new CancelCombatTaskPlanDocumentCommandHandler(repo.Object);

        var cmd = new CancelCombatTaskPlanDocumentCommand(
            DocumentId: Guid.NewGuid(),
            Reason: "Помилка",
            Author: "ui",
            NowUtc: DateTime.UtcNow);

        // Act
        await h.HandleAsync(cmd);

        // Assert
        repo.Verify(r => r.CancelAsync(cmd.DocumentId, cmd.Reason, cmd.Author, cmd.NowUtc, It.IsAny<CancellationToken>()), Times.Once);
    }
}
