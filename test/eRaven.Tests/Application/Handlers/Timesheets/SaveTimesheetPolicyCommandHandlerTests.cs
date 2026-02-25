//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SaveTimesheetPolicyCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Domain.ValueObjects;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class SaveTimesheetPolicyCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Throws_WhenCommandIsNull()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenCodeIdIsEmpty()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.Empty,
            Title: "Title",
            Description: null,
            SortOrder: 0,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: "tester",
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("CodeId", ex.Message);
    }

    [Theory]
    [InlineData("   ", "Назва коду")]
    [InlineData("\t\n", "Назва коду")]
    public async Task HandleAsync_Throws_WhenTitleIsBlank(string title, string messagePart)
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.NewGuid(),
            Title: title,
            Description: null,
            SortOrder: 0,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: "tester",
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains(messagePart, ex.Message);
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenSortOrderIsNegative()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.NewGuid(),
            Title: "Title",
            Description: null,
            SortOrder: -1,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: "tester",
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("SortOrder", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenPriorityIsNegative()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.NewGuid(),
            Title: "Title",
            Description: null,
            SortOrder: 0,
            Priority: -1,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: "tester",
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("Priority", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_Throws_WhenAuthorIsMissing(string? author)
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.NewGuid(),
            Title: "Title",
            Description: null,
            SortOrder: 0,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: author!,
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("Author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenNowUtcIsDefault()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: Guid.NewGuid(),
            Title: "Title",
            Description: null,
            SortOrder: 0,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [],
            Author: "tester",
            NowUtc: default);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("NowUtc", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NormalizesAndSavesPolicy()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var codeId = Guid.NewGuid();
        var to1 = Guid.NewGuid();
        var to2 = Guid.NewGuid();

        IReadOnlyCollection<TimesheetTransitionSpec>? captured = null;

        repo
            .Setup(x => x.SavePolicyAsync(
                codeId,
                "Title",
                "desc",
                10,
                20,
                true,
                It.IsAny<IReadOnlyCollection<TimesheetTransitionSpec>>(),
                "author",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, int, int, bool, IReadOnlyCollection<TimesheetTransitionSpec>, string, DateTime, CancellationToken>((
                _, _, _, _, _, _, allowed, _, _, _) => captured = allowed)
            .Returns(Task.CompletedTask);

        var command = new SaveTimesheetPolicyCommand(
            CodeId: codeId,
            Title: "  Title ",
            Description: "  desc  ",
            SortOrder: 10,
            Priority: 20,
            IsTerminal: true,
            AllowedTransitions:
            [
                new(to1, 0),
                new(to1, 1), // duplicate ToCodeId -> should be removed (keeps first)
                new(Guid.Empty, 0), // ignored
                new(codeId, 0), // ignored (from == to)
                null!, // ignored
                new(to2, 1)
            ],
            Author: "  author ",
            NowUtc: DateTime.UtcNow);

        // Act
        await handler.HandleAsync(command);

        // Assert
        repo.VerifyAll();
        Assert.NotNull(captured);

        // expects unique ToCodeId, without empty and without self
        var list = captured!.ToList();
        Assert.Equal(2, list.Count);

        Assert.Contains(list, x => x.ToCodeId == to1 && x.StartShiftDays == 0);
        Assert.Contains(list, x => x.ToCodeId == to2 && x.StartShiftDays == 1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public async Task HandleAsync_Throws_WhenStartShiftDaysOutOfRange(int startShiftDays)
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new SaveTimesheetPolicyCommandHandler(repo.Object);

        var codeId = Guid.NewGuid();

        var command = new SaveTimesheetPolicyCommand(
            CodeId: codeId,
            Title: "Title",
            Description: null,
            SortOrder: 0,
            Priority: 0,
            IsTerminal: false,
            AllowedTransitions: [new TimesheetTransitionSpecDto(Guid.NewGuid(), startShiftDays)],
            Author: "tester",
            NowUtc: DateTime.UtcNow);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(command));
        Assert.Contains("StartShiftDays", ex.Message);
    }
}
