//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonCardQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Handlers;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public class GetPersonCardQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_delegate_to_repository_and_return_result()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var id = Guid.NewGuid();

        var query = new GetPersonCardQuery(id);

        var expected = new PersonDto(
            Id: id,
            FullName: "John Doe",
            Rnokpp: "1234567890",
            Lifecycle: PersonLifecycle.Candidate,
            EnrollmentKind: null,
            Rank: null,
            Position: null,
            TemporaryPosition: null,
            PlannedPosition: null,
            EnrolledAt: null,
            ExcludedAt: null,
            UpdatedAtUtc: DateTime.UtcNow,
            Bzvp: null,
            Weapon: null,
            Callsign: null);

        using var cts = new CancellationTokenSource();

        repo.Setup(r => r.GetPersonByIdAsync(
                It.Is<Guid>(x => x == id),
                It.Is<CancellationToken>(ct => ct == cts.Token)))
            .ReturnsAsync(expected);

        var sut = new GetPersonCardQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query, cts.Token);

        // assert
        Assert.Same(expected, result);
        repo.VerifyAll();
    }
}
