//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskPersonLookupQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.Handlers.CombatTasks; // <-- підстав свій namespace
using eRaven.Application.Queries.CombatTasks;  // <-- підстав свій namespace
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskPersonLookupQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_delegate_to_repo_and_return_result()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskPersonLookupQueryHandler(repo.Object);

        var onDate = new DateOnly(2026, 02, 10);
        var query = new GetCombatTaskPersonLookupQuery(onDate);

        IReadOnlyList<ReadyCombatTaskPersonDto> expected = new[]
        {
            new ReadyCombatTaskPersonDto(
                PersonId: Guid.NewGuid(),
                Rnokpp: "1234567890",
                FullName: "Ivanov Ivan",
                Rank: "Сержант",
                Position: "Оператор",
                Weapon: "АК",
                Callsign: "FOX")
        };

        repo.Setup(x => x.GetFreePersonForMissionsAsync(onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetFreePersonForMissionsAsync(onDate, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskPersonLookupQueryHandler(repo.Object);

        var onDate = new DateOnly(2026, 02, 10);
        var query = new GetCombatTaskPersonLookupQuery(onDate);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        IReadOnlyList<ReadyCombatTaskPersonDto> expected = Array.Empty<ReadyCombatTaskPersonDto>();

        repo.Setup(x => x.GetFreePersonForMissionsAsync(onDate, ct))
            .ReturnsAsync(expected);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.Same(expected, result);

        repo.Verify(x => x.GetFreePersonForMissionsAsync(onDate, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_onDate_default_should_throw_and_not_call_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskPersonLookupQueryHandler(repo.Object);

        var query = new GetCombatTaskPersonLookupQuery(default);

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(query, CancellationToken.None));

        // assert
        Assert.Contains("OnDate", ex.Message, StringComparison.OrdinalIgnoreCase);
        repo.VerifyNoOtherCalls();
    }
}
