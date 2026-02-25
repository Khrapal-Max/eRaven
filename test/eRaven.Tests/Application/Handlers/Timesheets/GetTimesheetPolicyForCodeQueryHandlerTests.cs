//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyForCodeQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetPolicyForCodeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Throws_WhenCodeIdIsEmpty()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Loose);
        var handler = new GetTimesheetPolicyForCodeQueryHandler(repo.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new GetTimesheetPolicyForCodeQuery(Guid.Empty)));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenCodeDoesNotExist()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);
        var handler = new GetTimesheetPolicyForCodeQueryHandler(repo.Object);

        var codeId = Guid.NewGuid();
        repo
            .Setup(x => x.GetCodeByIdAsync(codeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((eRaven.Domain.Entities.TimesheetCodeDefinition?)null);

        // Act
        var result = await handler.HandleAsync(new GetTimesheetPolicyForCodeQuery(codeId));

        // Assert
        Assert.Null(result);
        repo.VerifyAll();
    }
}
