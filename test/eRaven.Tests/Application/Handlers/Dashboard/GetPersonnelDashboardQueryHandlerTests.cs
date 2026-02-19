//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonnelDashboardQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.DashboardRepository;
using eRaven.Application.Handlers.Dashboard;
using eRaven.Application.Queries.Dashboard;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Dashboard;

/// <summary>
/// Тести для <see cref="GetPersonnelDashboardQueryHandler"/>.
///
/// <para>Фіксуємо контракт:</para>
/// <list type="bullet">
/// <item><description>рахує Total/Unit/AttachedByList/AttachedByOrder по списку персоналу;</description></item>
/// <item><description>виставляє <c>GeneratedAtUtc</c> (UTC, “поточний час”).</description></item>
/// </list>
/// </summary>
public sealed class GetPersonnelDashboardQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ComputesCounts_ByEnrollmentKind()
    {
        // arrange
        var persons = new List<PersonReadModel>
        {
            NewEnrolled(EnrollmentKind.Unit),
            NewEnrolled(EnrollmentKind.Unit),
            NewEnrolled(EnrollmentKind.AttachedByList),
            NewEnrolled(EnrollmentKind.AttachedByOrder),
            NewEnrolled(null) // edge: kind can be null
        };

        var repo = new Mock<IDashboardRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetPersonnelDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons);

        var handler = new GetPersonnelDashboardQueryHandler(repo.Object);

        var t0 = DateTime.UtcNow;

        // act
        var dto = await handler.HandleAsync(new GetPersonnelDashboardQuery(), CancellationToken.None);

        var t1 = DateTime.UtcNow;

        // assert
        Assert.Equal(5, dto.TotalInTimesheet);
        Assert.Equal(2, dto.TimesheetUnit);
        Assert.Equal(1, dto.TimesheetOrder);
        Assert.Equal(1, dto.TimesheetBr);

        // GeneratedAtUtc is "now" (best-effort assertion)
        Assert.True(dto.GeneratedAtUtc >= t0 && dto.GeneratedAtUtc <= t1);

        repo.Verify(x => x.GetPersonnelDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeros_WhenNoPersons()
    {
        // arrange
        var repo = new Mock<IDashboardRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetPersonnelDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonReadModel>());

        var handler = new GetPersonnelDashboardQueryHandler(repo.Object);

        // act
        var dto = await handler.HandleAsync(new GetPersonnelDashboardQuery(), CancellationToken.None);

        // assert
        Assert.Equal(0, dto.TotalInTimesheet);
        Assert.Equal(0, dto.TimesheetUnit);
        Assert.Equal(0, dto.TimesheetOrder);
        Assert.Equal(0, dto.TimesheetBr);

        repo.Verify(x => x.GetPersonnelDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static PersonReadModel NewEnrolled(EnrollmentKind? kind)
        => new()
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1234567890",
            FullName = "Test Person",
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = kind
        };
}
