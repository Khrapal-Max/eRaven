//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlersTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Person;
using eRaven.Application.EventJson;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Presenter;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Events.PersonEvents.Info;
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

        // json не викликається, бо void-подій нема
        var json = new Mock<IEventJson>(MockBehavior.Loose);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object, json.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        var e1 = new PersonEventDto(
            Version: 1,
            EventId: Guid.NewGuid(),
            EventType: "PersonCreated",
            PayloadJson: "{}",
            Author: "tester",
            OccurredAtUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc),
            EffectiveDate: null);

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([e1]);

        var mapped = new PersonEventListItemDto(
            Version: e1.Version,
            EventId: e1.EventId,
            EffectiveDate: e1.EffectiveDate,
            Title: "Створено картку",
            Details: "…",
            Author: e1.Author,
            OccurredAtUtc: e1.OccurredAtUtc,
            IsVoided: false
        );

        presenter.Setup(x => x.ToListItem(e1))
            .Returns(mapped);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Single(result);
        Assert.Equal(mapped, result[0]); // record equality, не Same

        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        presenter.Verify(x => x.ToListItem(e1), Times.Once);

        repo.VerifyNoOtherCalls();
        presenter.VerifyNoOtherCalls();
        // json може взагалі не викликатись
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);
        var json = new Mock<IEventJson>(MockBehavior.Loose);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object, json.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.GetHistoryAsync(personId, ct))
            .ReturnsAsync([]);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.Empty(result);

        repo.Verify(x => x.GetHistoryAsync(personId, ct), Times.Once);
        repo.VerifyNoOtherCalls();
        presenter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_returns_multiple_events_should_map_each_event_and_sort_by_version_desc()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);
        var json = new Mock<IEventJson>(MockBehavior.Loose);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object, json.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        // repo повертає в довільному порядку
        var e1 = new PersonEventDto(1, Guid.NewGuid(), "A", "{}", "u", DateTime.UtcNow, null);
        var e2 = new PersonEventDto(2, Guid.NewGuid(), "B", "{}", "u", DateTime.UtcNow, new DateOnly(2026, 1, 1));

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([e1, e2]);

        var m1 = new PersonEventListItemDto(1, e1.EventId, e1.EffectiveDate, "t1", "d1", e1.Author, e1.OccurredAtUtc, false);
        var m2 = new PersonEventListItemDto(2, e2.EventId, e2.EffectiveDate, "t2", "d2", e2.Author, e2.OccurredAtUtc, false);

        presenter.Setup(x => x.ToListItem(e1)).Returns(m1);
        presenter.Setup(x => x.ToListItem(e2)).Returns(m2);

        // act
        var result = await sut.HandleAsync(query);

        // assert (має бути DESC)
        Assert.Equal(2, result.Count);
        Assert.Equal(m2, result[0]);
        Assert.Equal(m1, result[1]);

        presenter.Verify(x => x.ToListItem(e1), Times.Once);
        presenter.Verify(x => x.ToListItem(e2), Times.Once);
        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);

        repo.VerifyNoOtherCalls();
        presenter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_hide_void_events_and_mark_target_event_as_voided()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var presenter = new Mock<IPersonEventPresenter>(MockBehavior.Strict);
        var json = new Mock<IEventJson>(MockBehavior.Strict);

        var sut = new GetPersonHistoryQueryHandler(repo.Object, presenter.Object, json.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonHistoryQuery(personId);

        var targetId = Guid.NewGuid();

        var targetEvent = new PersonEventDto(
            Version: 10,
            EventId: targetId,
            EventType: "PersonRankChanged",
            PayloadJson: "{...}",
            Author: "u",
            OccurredAtUtc: new DateTime(2026, 01, 10, 10, 0, 0, DateTimeKind.Utc),
            EffectiveDate: new DateOnly(2026, 1, 1));

        var voidEvent = new PersonEventDto(
            Version: 11,
            EventId: Guid.NewGuid(),
            EventType: nameof(PersonEventVoided), // endswith => void
            PayloadJson: "{void}",
            Author: "admin",
            OccurredAtUtc: new DateTime(2026, 01, 20, 10, 0, 0, DateTimeKind.Utc),
            EffectiveDate: null);

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([targetEvent, voidEvent]);

        // presenter викликається лише для НЕ-void події
        var mappedTarget = new PersonEventListItemDto(
            Version: targetEvent.Version,
            EventId: targetEvent.EventId,
            EffectiveDate: targetEvent.EffectiveDate,
            Title: "Зміна звання",
            Details: "На: сержант",
            Author: targetEvent.Author,
            OccurredAtUtc: targetEvent.OccurredAtUtc,
            IsVoided: false);

        presenter.Setup(x => x.ToListItem(targetEvent)).Returns(mappedTarget);

        // json викликається лише для void-події
        json.Setup(x => x.TryDeserialize<PersonEventVoided>(voidEvent.PayloadJson))
            .Returns(new PersonEventVoided(
                EventId: voidEvent.EventId,
                AggregateId: personId,
                TargetEventId: targetId,
                Reason: "помилка",
                Author: "admin",
                OccurredAtUtc: voidEvent.OccurredAtUtc));

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Single(result);

        // має бути новий record через `with { IsVoided = true }`
        Assert.Equal(mappedTarget with { IsVoided = true }, result[0]);
        Assert.True(result[0].IsVoided);

        presenter.Verify(x => x.ToListItem(targetEvent), Times.Once);
        presenter.VerifyNoOtherCalls();

        json.Verify(x => x.TryDeserialize<PersonEventVoided>(voidEvent.PayloadJson), Times.Once);

        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}