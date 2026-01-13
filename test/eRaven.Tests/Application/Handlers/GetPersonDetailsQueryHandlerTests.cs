//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonDetailsQueryHandlerTests (for positional record PersonDetailsDto)
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Handlers;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public sealed class GetPersonDetailsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_dto()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonDetailsQueryHandler(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        var expected = new PersonDetailsDto(
            Id: personId,
            Lifecycle: PersonLifecycle.Reserved,
            EnrollmentKind: null,
            EnrollmentReference: null,
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: "Ivanovich",
            FullName: "Ivanov Ivan Ivanovich",
            Rank: "Сержант",
            PositionSort: 10,
            Position: "Стрілець",
            Bzvp: null,
            Weapon: null,
            Callsign: null,
            EnrolledAt: null,
            ExcludedAt: null,
            Version: 1,
            UpdatedAtUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        );

        repo.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.NotNull(result);
        Assert.Equal(expected, result); // для record це ок
        Assert.Equal(personId, result!.Id);
        Assert.Equal("1234567890", result.Rnokpp);

        repo.Verify(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_when_repo_returns_null_should_return_null()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonDetailsQueryHandler(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        repo.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDetailsDto?)null);

        // act
        var result = await sut.HandleAsync(query);

        // assert
        Assert.Null(result);

        repo.Verify(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_should_pass_cancellation_token_to_repo()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonDetailsQueryHandler(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var expected = new PersonDetailsDto(
            Id: personId,
            Lifecycle: PersonLifecycle.Reserved,
            EnrollmentKind: null,
            EnrollmentReference: null,
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            FullName: "Ivanov Ivan",
            Rank: null,
            PositionSort: null,
            Position: null,
            Bzvp: null,
            Weapon: null,
            Callsign: null,
            EnrolledAt: null,
            ExcludedAt: null,
            Version: 1,
            UpdatedAtUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        );

        repo.Setup(x => x.GetByIdAsync(personId, ct))
            .ReturnsAsync(expected);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);

        repo.Verify(x => x.GetByIdAsync(personId, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
