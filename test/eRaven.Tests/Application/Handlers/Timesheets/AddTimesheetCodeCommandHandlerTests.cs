//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class AddTimesheetCodeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsRepoAndReturnsId()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        var expectedId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var command = new AddTimesheetCodeCommand(
            Code: "30",
            Title: "Готовність",
            Description: "desc",
            SortOrder: 10,
            Priority: 20,
            IsTerminal: false,
            RoleCode: RoleCode.TransitionCode,
            UiStyle: TimesheetUiStyle.Ready,
            Author: "tester",
            NowUtc: nowUtc);

        repo
            .Setup(x => x.AddCodeAsync(
                command.Code,
                command.Title,
                command.Description,
                command.SortOrder,
                command.Priority,
                command.IsTerminal,
                command.RoleCode,
                command.UiStyle,
                command.Author,
                command.NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var handler = new AddTimesheetCodeCommandHandler(repo.Object);

        // Act
        var id = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(expectedId, id);
        repo.VerifyAll();
    }
}
