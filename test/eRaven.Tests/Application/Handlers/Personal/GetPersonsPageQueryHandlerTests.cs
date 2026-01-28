//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQueryHandlerTests
//-----------------------------------------------------------------------------


using eRaven.Application.DTOs.Person;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class GetPersonsPageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_delegate_to_repo_and_return_result()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var query = new GetPersonsPageQuery(
            Page: 2,
            PageSize: 8,
            Search: "ivan",
            AsOfDate: null,
            Lifecycle: null,
            EnrollmentKind: null);

        var expected = new PagedResult<PersonListItemDto>(
            Items:
            [
                new PersonListItemDto(
                    Id: Guid.NewGuid(),
                    FullName: "Ivanov Ivan",
                    Rnokpp: "1234567890",
                    Lifecycle: PersonLifecycle.Reserved,
                    Rank: "сержант",
                    PositionSort: 5,
                    Position: "Оператор",
                    EnrollmentKind: null,
                    EnrolledAt: null,
                    ExcludedAt: null,
                    UpdatedAtUtc: DateTime.UtcNow)
            ],
            Page: 2,
            PageSize: 8,
            TotalCount: 25);

        repo.Setup(r => r.GetPageAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GetPersonsPageQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert
        Assert.Same(expected, result); // повертаємо той самий інстанс (бо хендлер не мапить)
        repo.Verify(r => r.GetPageAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}