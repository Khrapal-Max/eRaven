//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonalHistoriesQueryHandlersTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Queries.Personal;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class GetPersonalHistoriesQueryHandlersTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_history()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonalHistoriesQueryHandlers(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        IReadOnlyList<PersonEventDto> expected =
        [
            new PersonEventDto(
                Version: 1,
                EventId: Guid.NewGuid(),
                EventType: "PersonCreated",
                PayloadJson: "{}",
                Author: "tester",
                OccurredAtUtc: new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc),
                EffectiveDate: null)
        ];

        repo.Setup(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Same(expected, result);
        repo.Verify(x => x.GetHistoryAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonalHistoriesQueryHandlers(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        repo.Setup(x => x.GetHistoryAsync(personId, ct))
            .ReturnsAsync(Array.Empty<PersonEventDto>());

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.Empty(result);
        repo.Verify(x => x.GetHistoryAsync(personId, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
