//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyCodesQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries.Timesheets;
using Moq;

namespace eRaven.Tests.Application.Handlers.Timesheets;

public sealed class GetTimesheetPolicyCodesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsRepoWithIncludeInactive_AndMapsEmpty()
    {
        // Arrange
        var repo = new Mock<ITimesheetPolicyRepository>(MockBehavior.Strict);

        repo
            .Setup(x => x.GetCodesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetTimesheetPolicyCodesQueryHandler(repo.Object);
        var query = new GetTimesheetPolicyCodesQuery(IncludeInactive: true);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        repo.VerifyAll();
    }
}
