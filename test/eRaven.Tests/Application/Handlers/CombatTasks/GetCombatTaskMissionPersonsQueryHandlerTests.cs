//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetCombatTaskMissionPersonsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Handlers.CombatTasks;   
using eRaven.Application.Queries.CombatTasks;  
using eRaven.Domain.Entities;                    
using eRaven.Domain.Enums;                       
using Moq;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

public sealed class GetCombatTaskMissionPersonsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_delegate_to_repo_and_map_to_dto()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskMissionPersonsQueryHandler(repo.Object);

        var missionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 02, 20);
        var query = new GetCombatTaskMissionPersonsQuery(missionId, onDate);

        var spanId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var rows = new List<TimesheetTaskSpan>
        {
            new()
            {
                Id = spanId,
                TimesheetId = Guid.NewGuid(),
                PersonId = personId,
                MissionId = missionId,

                OpenedByCombatTaskDocumentId = Guid.NewGuid(),
                FromDate = new DateOnly(2026, 02, 18),
                ToDate = null,
                Status = DocumentStatus.Active,

                Rnokpp = "1234567890",
                FullName = "Ivanov Ivan",
                Rank = "Сержант",
                Position = "Оператор",
                Weapon = "АК",
                Callsign = "FOX",

                CreatedBy = "seed",
                CreatedAtUtc = new DateTime(2026, 02, 18, 10, 0, 0, DateTimeKind.Utc),
                UpdatedBy = "seed",
                UpdatedAtUtc = new DateTime(2026, 02, 18, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        repo.Setup(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Single(result);
        var dto = result[0];

        // ⚠️ Фіксуємо поточну реалізацію: PersonId береться зі span.Id (можливо це баг)
        Assert.Equal(personId, dto.PersonId);

        Assert.Equal("1234567890", dto.Rnokpp);
        Assert.Equal("Ivanov Ivan", dto.FullName);
        Assert.Equal("FOX", dto.Callsign);
        Assert.Equal("Сержант", dto.Rank);
        Assert.Equal("Оператор", dto.Position);
        Assert.Equal("АК", dto.Weapon);
        Assert.Equal(new DateOnly(2026, 02, 18), dto.From);

        repo.Verify(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_return_empty_when_repo_returns_empty()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskMissionPersonsQueryHandler(repo.Object);

        var missionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 02, 20);
        var query = new GetCombatTaskMissionPersonsQuery(missionId, onDate);

        repo.Setup(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Empty(result);

        repo.Verify(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<ITimesheetMissionPlanningRepository>(MockBehavior.Strict);
        var sut = new GetCombatTaskMissionPersonsQueryHandler(repo.Object);

        var missionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 02, 20);
        var query = new GetCombatTaskMissionPersonsQuery(missionId, onDate);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, ct))
            .ReturnsAsync([]);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.Empty(result);

        repo.Verify(x => x.GetActiveMissionClosablePersonsAsync(missionId, onDate, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
