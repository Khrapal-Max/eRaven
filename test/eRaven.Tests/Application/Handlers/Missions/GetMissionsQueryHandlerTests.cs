//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetMissionsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Handlers.Missions;
using eRaven.Application.Queries.Missions;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Missions;

public sealed class GetMissionsQueryHandlerTests
{
    [Fact(DisplayName = "GetMissionsQuery: фільтрує OnlyOpen/Mode/Search та сортує (open first, CreatedAt desc)")]
    public async Task HandleAsync_FiltersAndSorts_ReturnsDtos()
    {
        // Arrange
        var repo = new Mock<IMissionRepository>(MockBehavior.Strict);

        var openNew = new eRaven.Domain.Entities.Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = "Район-A",
            NamePoint = "Точка-1",
            TypeDrone = "DJI",
            Target = "Розвідка",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 01, 10),
            ClosedAt = null
        };

        var openOld = new eRaven.Domain.Entities.Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = "Район-A",
            NamePoint = "Точка-2",
            TypeDrone = "Autel",
            Target = "Розвідка",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 01, 05),
            ClosedAt = null
        };

        var closedMatch = new eRaven.Domain.Entities.Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = "Район-A",
            NamePoint = "Точка-3",
            TypeDrone = "DJI",
            Target = "Розвідка",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 01, 12),
            ClosedAt = new DateOnly(2026, 01, 12) // закрита => має відфільтруватися OnlyOpen
        };

        var openDifferentMode = new eRaven.Domain.Entities.Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = "Район-A",
            NamePoint = "Точка-4",
            TypeDrone = "DJI",
            Target = "Розвідка",
            MissionMode = MissionMode.Night, // інший режим => має відфільтруватися Mode
            CreatedAt = new DateOnly(2026, 01, 11),
            ClosedAt = null
        };

        var openDifferentSearch = new eRaven.Domain.Entities.Mission
        {
            Id = Guid.NewGuid(),
            PositionArea = "Район-B",
            NamePoint = "Точка-5",
            TypeDrone = "Parrot",
            Target = "Охорона",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 01, 09),
            ClosedAt = null // не співпадає по search "Розвідка"/"DJI"/"Район-A"
        };

        repo.Setup(r => r.GetMissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                openOld,
                closedMatch,
                openDifferentMode,
                openNew,
                openDifferentSearch
            ]);

        var handler = new GetMissionsQueryHandler(repo.Object);

        var query = new GetMissionsQuery(
            OnlyOpen: true,
            Search: "  dji  ", // перевіряємо Trim + OrdinalIgnoreCase
            Mode: MissionMode.Day
        );

        // Act
        var rows = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        // Має лишитися тільки openNew + openOld (Day + open + search contains "dji")
        Assert.Single(rows);

        // Перевіряємо сортування: open first (всі open), далі CreatedAt desc => openNew потім openOld
        Assert.Equal(openNew.Id, rows[0].MissionId);

        // Перевіряємо мапінг DTO
        Assert.True(rows[0].IsOpen);
        Assert.Equal("Район-A", rows[0].PositionArea);
        Assert.Equal("Точка-1", rows[0].NamePoint);
        Assert.Equal("DJI", rows[0].DroneName);
        Assert.Equal("Розвідка", rows[0].Target);
        Assert.Equal(MissionMode.Day, rows[0].MissionMode);
        Assert.Equal(new DateOnly(2026, 01, 10), rows[0].CreatedAt);
        Assert.Null(rows[0].ClosedAt);

        repo.Verify(r => r.GetMissionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
