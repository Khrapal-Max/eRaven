//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQueryHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Handlers;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.PersonRepository;
using Moq;

namespace eRaven.Tests.Application.Handlers;

public sealed class GetPersonsPageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_should_delegate_to_repository_and_return_result()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var q = new GetPersonsPageQuery(
            Page: 2,
            PageSize: 10,
            Search: "Ivan",
            AsOfDate: new DateOnly(2026, 01, 10),
            Lifecycle: PersonLifecycle.Enrolled,
            EnrollmentKind: EnrollmentKind.Unit);

        var items = new List<PersonTableDto>
        {
            new(
                Id: Guid.NewGuid(),
                FullName: "Ivanov Ivan",
                Rnokpp: "1234567890",
                Lifecycle: PersonLifecycle.Enrolled,
                EnrollmentKind: EnrollmentKind.Unit,
                Rank: "Солдат",
                Position: "Стрілець",
                TemporaryPosition: null,
                PlannedPosition: null,
                EnrolledAt: new DateOnly(2026, 01, 10),
                ExcludedAt: null,
                UpdatedAtUtc: new DateTime(2026, 01, 10, 12, 0, 0, DateTimeKind.Utc)
            )
        };

        var expected = new PagedResult<PersonTableDto>(
            Items: items,
            Page: q.Page,
            PageSize: q.PageSize,
            TotalCount: 123);

        using var cts = new CancellationTokenSource();

        repo.Setup(r => r.GetPersonsPageAsync(
                It.Is<GetPersonsPageQuery>(x => x == q),
                It.Is<CancellationToken>(ct => ct == cts.Token)))
            .ReturnsAsync(expected);

        var sut = new GetPersonsPageQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(q, cts.Token);

        // assert
        Assert.Same(expected, result);
        repo.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_when_called_without_ct_should_use_CancellationTokenNone()
    {
        // arrange
        var repo = new Mock<IPersonRepository>(MockBehavior.Strict);

        var q = new GetPersonsPageQuery(Page: 1, PageSize: 50);

        var expected = new PagedResult<PersonTableDto>(
            Items: [],
            Page: 1,
            PageSize: 50,
            TotalCount: 0);

        repo.Setup(r => r.GetPersonsPageAsync(
                It.Is<GetPersonsPageQuery>(x => x == q),
                It.Is<CancellationToken>(ct => ct == CancellationToken.None)))
            .ReturnsAsync(expected);

        var sut = new GetPersonsPageQueryHandler(repo.Object);

        // act
        var result = await sut.HandleAsync(q);

        // assert
        Assert.Same(expected, result);
        repo.VerifyAll();
    }
}
