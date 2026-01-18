//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonnelDashboardQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.Handlers.Dashboard;
using eRaven.Application.Queries.Dashboard;
using eRaven.Infrastructure.Repositories.DashboardRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Dashboard;

public sealed class GetPersonnelDashboardQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_maps_snapshot_to_dto_and_sets_generated_at()
    {
        // Arrange
        var ct = new CancellationTokenSource().Token;

        var snap = new PersonnelDashboardSnapshot(
            TotalInTimesheet: 10,
            TimesheetUnit: 4,
            TimesheetOrder: 3,
            TimesheetBr: 3
        );

        var repo = new Mock<IDashboardRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetPersonnelDashboardAsync(ct))
            .ReturnsAsync(snap);

        var handler = new GetPersonnelDashboardQueryHandler(repo.Object);

        var before = DateTime.UtcNow;

        // Act
        var dto = await handler.HandleAsync(new GetPersonnelDashboardQuery(), ct);

        var after = DateTime.UtcNow;

        // Assert
        Assert.Equal(10, dto.TotalInTimesheet);
        Assert.Equal(4, dto.TimesheetUnit);
        Assert.Equal(3, dto.TimesheetOrder);
        Assert.Equal(3, dto.TimesheetBr);

        Assert.NotEqual(default, dto.GeneratedAtUtc);
        Assert.True(dto.GeneratedAtUtc >= before && dto.GeneratedAtUtc <= after,
            $"GeneratedAtUtc '{dto.GeneratedAtUtc:o}' must be within test execution window.");

        repo.Verify(x => x.GetPersonnelDashboardAsync(ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
