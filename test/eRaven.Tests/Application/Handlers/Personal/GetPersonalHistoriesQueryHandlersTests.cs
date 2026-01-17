//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlersTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Presenter;
using eRaven.Application.Queries.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class GetPersonalHistoriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_map_events_via_presenter_and_return_list_items()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        var raw = new List<PersonEventDto>
        {
            new(
                Version: 1,
                EventId: Guid.NewGuid(),
                EventType: "PersonCreated",
                PayloadJson: "{}",
                Author: "tester",
                OccurredAtUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc),
                EffectiveDate: null)
        };

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        var mapped = new PersonEventListItemDto(
            Version: raw[0].Version,
            EventId: raw[0].EventId,
            EffectiveDate: raw[0].EffectiveDate,
            Title: "Створено картку",
            Details: "…",
            Author: raw[0].Author,
            OccurredAtUtc: raw[0].OccurredAtUtc
        );

        presenter.Setup(x => x.ToListItem(raw[0]))
            .Returns(mapped);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Same(mapped, result[0]); // same instance that presenter returned

        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        presenter.Verify(x => x.ToListItem(raw[0]), Times.Once);

        repo.VerifyNoOtherCalls();
        presenter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.GetHistoryAsync(personId, ct))
            .ReturnsAsync([]);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.NotNull(result);
        Assert.Empty(result);

        repo.Verify(x => x.GetHistoryAsync(personId, ct), Times.Once);
        repo.VerifyNoOtherCalls();

        // presenter не має викликатись, бо raw порожній
        presenter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_returns_multiple_events_should_map_each_event()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        var e1 = new PersonEventDto(1, Guid.NewGuid(), "A", "{}", "u", DateTime.UtcNow, null);
        var e2 = new PersonEventDto(2, Guid.NewGuid(), "B", "{}", "u", DateTime.UtcNow, new DateOnly(2026, 1, 1));

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([e1, e2]);

        var m1 = new PersonEventListItemDto(1, e1.EventId, e1.EffectiveDate, "t1", "d1", e1.Author, e1.OccurredAtUtc);
        var m2 = new PersonEventListItemDto(2, e2.EventId, e2.EffectiveDate, "t2", "d2", e2.Author, e2.OccurredAtUtc);

        presenter.Setup(x => x.ToListItem(e1)).Returns(m1);
        presenter.Setup(x => x.ToListItem(e2)).Returns(m2);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Equal(2, result.Count);
        Assert.Same(m1, result[0]);
        Assert.Same(m2, result[1]);

        presenter.Verify(x => x.ToListItem(e1), Times.Once);
        presenter.Verify(x => x.ToListItem(e2), Times.Once);
        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        presenter.VerifyNoOtherCalls();
    }
}