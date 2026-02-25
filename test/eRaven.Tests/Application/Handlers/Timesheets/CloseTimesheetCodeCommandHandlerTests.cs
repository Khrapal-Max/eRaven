//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseTimesheetCodeCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.Handlers.Timesheets;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class CloseTimesheetCodeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsRepo()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var nowUtc = DateTime.UtcNow;
        var command = new CloseTimesheetCodeCommand(
            CodeId: Guid.NewGuid(),
            Author: "tester",
            NowUtc: nowUtc);

        repo
            .Setup(x => x.CloseCodeAsync(
                command.CodeId,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CloseTimesheetCodeCommandHandler(repo.Object);

        // Act
        await handler.HandleAsync(command);

        // Assert
        repo.VerifyAll();
    }
}
