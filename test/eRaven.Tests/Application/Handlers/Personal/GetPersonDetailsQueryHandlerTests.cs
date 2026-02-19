//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonDetailsQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class GetPersonDetailsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_call_repo_and_return_mapped_dto()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);
        var sut = new GetPersonDetailsQueryHandler(repo.Object);

        var personId = Guid.NewGuid();
        var query = new GetPersonDetailsQuery(personId);

        var model = new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Reserved,
            EnrollmentKind = null,
            EnrollmentReference = null,
            Rnokpp = "1234567890",
            LastName = "Ivanov",
            FirstName = "Ivan",
            MiddleName = "Ivanovich",
            FullName = "Ivanov Ivan Ivanovich",
            Rank = "Сержант",
            PositionSort = 10,
            Position = "Стрілець",
            Bzvp = "BZVP-1",
            Weapon = "AK",
            Callsign = "FOX",
            EnrolledAt = null,
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        };

        repo.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.NotNull(result);

        Assert.Equal(model.Id, result!.Id);
        Assert.Equal(model.Lifecycle, result.Lifecycle);
        Assert.Equal(model.EnrollmentKind, result.EnrollmentKind);
        Assert.Equal(model.EnrollmentReference, result.EnrollmentReference);

        Assert.Equal(model.Rnokpp, result.Rnokpp);
        Assert.Equal(model.LastName, result.LastName);
        Assert.Equal(model.FirstName, result.FirstName);
        Assert.Equal(model.MiddleName, result.MiddleName);
        Assert.Equal(model.FullName, result.FullName);

        Assert.Equal(model.Rank, result.Rank);
        Assert.Equal(model.PositionSort, result.PositionSort);
        Assert.Equal(model.Position, result.Position);

        Assert.Equal(model.Bzvp, result.Bzvp);
        Assert.Equal(model.Weapon, result.Weapon);
        Assert.Equal(model.Callsign, result.Callsign);

        Assert.Equal(model.EnrolledAt, result.EnrolledAt);
        Assert.Equal(model.ExcludedAt, result.ExcludedAt);

        Assert.Equal(model.Version, result.Version);
        Assert.Equal(model.UpdatedAtUtc, result.UpdatedAtUtc);

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
            .ReturnsAsync((PersonReadModel?)null);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

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

        var model = new PersonReadModel
        {
            Id = personId,
            Lifecycle = PersonLifecycle.Reserved,
            EnrollmentKind = null,
            EnrollmentReference = null,
            Rnokpp = "1234567890",
            LastName = "Ivanov",
            FirstName = "Ivan",
            MiddleName = null,
            FullName = "Ivanov Ivan",
            Rank = null,
            PositionSort = null,
            Position = null,
            Bzvp = null,
            Weapon = null,
            Callsign = null,
            EnrolledAt = null,
            ExcludedAt = null,
            Version = 1,
            UpdatedAtUtc = new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        };

        repo.Setup(x => x.GetByIdAsync(personId, ct))
            .ReturnsAsync(model);

        // act
        var result = await sut.HandleAsync(query, ct);

        // assert
        Assert.NotNull(result);
        Assert.Equal(personId, result!.Id);
        Assert.Equal(model.FullName, result.FullName);

        repo.Verify(x => x.GetByIdAsync(personId, ct), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}