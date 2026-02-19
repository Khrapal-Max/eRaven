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
using eRaven.Domain.Entities;
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

        var eventId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc);

        var r1 = NewRecord(
            aggregateId: personId,
            version: 1,
            eventId: eventId,
            eventType: "PersonCreated",
            payloadJson: "{}",
            author: "tester",
            occurredAtUtc: occurredAt,
            effectiveDate: null);

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([r1]);

        var mapped = new PersonEventListItemDto(
            Version: 1,
            EventId: eventId,
            EffectiveDate: null,
            Title: "Створено картку",
            Details: "…",
            Author: "tester",
            OccurredAtUtc: occurredAt,
            IsVoided: false);

        presenter.Setup(x => x.ToListItem(It.Is<PersonEventDto>(e =>
                e.Version == 1 &&
                e.EventId == eventId &&
                e.EventType == "PersonCreated" &&
                e.PayloadJson == "{}" &&
                e.Author == "tester" &&
                e.OccurredAtUtc == occurredAt &&
                e.EffectiveDate == null)))
            .Returns(mapped);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Single(result);
        Assert.Equal(mapped, result[0]); // record equality

        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        presenter.Verify(x => x.ToListItem(It.IsAny<PersonEventDto>()), Times.Once);

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

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var t = DateTime.UtcNow;

        var r1 = NewRecord(personId, 1, id1, "A", "{}", "u", t, null);
        var r2 = NewRecord(personId, 2, id2, "B", "{}", "u", t.AddMinutes(1), new DateOnly(2026, 1, 1));

        // repo вже повертає ORDER BY Version, але нам це не критично: хендлер все одно повертає DESC
        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([r1, r2]);

        var m1 = new PersonEventListItemDto(1, id1, null, "t1", "d1", "u", t, false);
        var m2 = new PersonEventListItemDto(2, id2, new DateOnly(2026, 1, 1), "t2", "d2", "u", t.AddMinutes(1), false);

        presenter.Setup(x => x.ToListItem(It.Is<PersonEventDto>(e => e.Version == 1 && e.EventId == id1 && e.EventType == "A")))
            .Returns(m1);

        presenter.Setup(x => x.ToListItem(It.Is<PersonEventDto>(e => e.Version == 2 && e.EventId == id2 && e.EventType == "B")))
            .Returns(m2);

        // act
        var result = await sut.HandleAsync(query);

        // assert (має бути DESC)
        Assert.Equal(2, result.Count);
        Assert.Equal(m2, result[0]);
        Assert.Equal(m1, result[1]);

        presenter.Verify(x => x.ToListItem(It.IsAny<PersonEventDto>()), Times.Exactly(2));
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
        var voidId = Guid.NewGuid();

        var targetOccurred = new DateTime(2026, 01, 10, 10, 0, 0, DateTimeKind.Utc);
        var voidOccurred = new DateTime(2026, 01, 20, 10, 0, 0, DateTimeKind.Utc);

        var targetRecord = NewRecord(
            aggregateId: personId,
            version: 10,
            eventId: targetId,
            eventType: "PersonRankChanged",
            payloadJson: "{...}",
            author: "u",
            occurredAtUtc: targetOccurred,
            effectiveDate: new DateOnly(2026, 1, 1));

        // важливо: EndsWith(nameof(PersonEventVoided)) має спрацювати
        var voidRecord = NewRecord(
            aggregateId: personId,
            version: 11,
            eventId: voidId,
            eventType: typeof(PersonEventVoided).FullName ?? nameof(PersonEventVoided),
            payloadJson: "{void}",
            author: "admin",
            occurredAtUtc: voidOccurred,
            effectiveDate: null);

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([targetRecord, voidRecord]);

        // presenter викликається лише для НЕ-void події
        var mappedTarget = new PersonEventListItemDto(
            Version: 10,
            EventId: targetId,
            EffectiveDate: new DateOnly(2026, 1, 1),
            Title: "Зміна звання",
            Details: "На: сержант",
            Author: "u",
            OccurredAtUtc: targetOccurred,
            IsVoided: false);

        presenter.Setup(x => x.ToListItem(It.Is<PersonEventDto>(e =>
                e.Version == 10 &&
                e.EventId == targetId &&
                e.EventType == "PersonRankChanged")))
            .Returns(mappedTarget);

        // json викликається лише для void-події
        json.Setup(x => x.TryDeserialize<PersonEventVoided>("{void}"))
            .Returns(new PersonEventVoided(
                EventId: voidId,
                AggregateId: personId,
                TargetEventId: targetId,
                Reason: "помилка",
                Author: "admin",
                OccurredAtUtc: voidOccurred));

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Single(result);

        // має бути новий record через `with { IsVoided = true }`
        Assert.Equal(mappedTarget with { IsVoided = true }, result[0]);
        Assert.True(result[0].IsVoided);

        presenter.Verify(x => x.ToListItem(It.IsAny<PersonEventDto>()), Times.Once);
        presenter.VerifyNoOtherCalls();

        json.Verify(x => x.TryDeserialize<PersonEventVoided>("{void}"), Times.Once);

        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static PersonEventRecord NewRecord(
        Guid aggregateId,
        int version,
        Guid eventId,
        string eventType,
        string payloadJson,
        string author,
        DateTime occurredAtUtc,
        DateOnly? effectiveDate)
        => new()
        {
            AggregateId = aggregateId,
            Version = version,
            EventId = eventId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Author = author,
            OccurredAtUtc = occurredAtUtc,
            EffectiveDate = effectiveDate
        };
}
