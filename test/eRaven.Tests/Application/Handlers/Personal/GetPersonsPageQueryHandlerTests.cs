//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Moq;

namespace eRaven.Tests.Application.Handlers.Personal;

public sealed class GetPersonsPageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_normalize_params_delegate_to_repo_and_map_items()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var query = new GetPersonsPageQuery(
            Page: 0,                 // invalid -> normalized to 1
            PageSize: 500,           // invalid -> normalized to 25 (per handler rule)
            Search: "  ivan  ",      // -> trimmed to "ivan"
            AsOfDate: null,
            Lifecycle: null,
            EnrollmentKind: null);

        var p1 = new PersonReadModel
        {
            Id = Guid.NewGuid(),
            FullName = "Ivanov Ivan",
            Rnokpp = "1234567890",
            Lifecycle = PersonLifecycle.Reserved,
            Rank = "сержант",
            PositionSort = 5,
            Position = "Оператор",
            EnrollmentKind = null,
            EnrolledAt = null,
            ExcludedAt = null,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var repoResult = new PagedResult<PersonReadModel>(
            Items: [p1],
            Page: 1,
            PageSize: 25,
            TotalCount: 25);

        repo.Setup(r => r.GetPageAsync(
                page: 1,
                pageSize: 25,
                search: "ivan",
                asOfDate: null,
                lifecycle: null,
                enrollmentKind: null,
                ct: It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        var sut = new GetPersonsPageQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // assert: metadata
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(25, result.TotalCount);

        // assert: mapping
        Assert.Single(result.Items);
        var dto = result.Items[0];

        Assert.Equal(p1.Id, dto.Id);
        Assert.Equal(p1.FullName, dto.FullName);
        Assert.Equal(p1.Rnokpp, dto.Rnokpp);
        Assert.Equal(PersonLifecycleDto.Reserved, dto.Lifecycle);
        Assert.Equal(p1.Rank, dto.Rank);
        Assert.Equal(p1.PositionSort, dto.PositionSort);
        Assert.Equal(p1.Position, dto.Position);
        Assert.Null(dto.EnrollmentKind);
        Assert.Equal(p1.EnrolledAt, dto.EnrolledAt);
        Assert.Equal(p1.ExcludedAt, dto.ExcludedAt);
        Assert.Equal(p1.UpdatedAtUtc, dto.UpdatedAtUtc);

        // verify repo call
        repo.Verify(r => r.GetPageAsync(
                1, 25, "ivan", null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);

        repo.VerifyNoOtherCalls();
    }
}
