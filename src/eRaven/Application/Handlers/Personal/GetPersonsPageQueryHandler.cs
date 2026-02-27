//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;

namespace eRaven.Application.Handlers.Personal;

/// <summary>
/// Повертає картки, з кількісю сторінок
/// </summary>
public sealed class GetPersonsPageQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>
{
    private readonly IPersonRepository _repo = repo;

    /// <inheritdoc />
    public async Task<PagedResult<PersonListItemDto>> HandleAsync(GetPersonsPageQuery query, CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize is < 1 or > 200 ? 25 : query.PageSize;

        var pageData = await _repo.GetPageAsync(
            page: page,
            pageSize: size,
            search: search,
            asOfDate: query.AsOfDate,
            lifecycle: PersonMissionEnumDtoMapper.ToDomain(query.Lifecycle),
            enrollmentKind: PersonMissionEnumDtoMapper.ToDomain(query.EnrollmentKind),
            ct: ct);

        var items = pageData.Items
            .Select(x => new PersonListItemDto(
                x.Id,
                x.FullName,
                x.Rnokpp,
                PersonMissionEnumDtoMapper.ToDto(x.Lifecycle),
                x.Rank,
                x.PositionSort,
                x.Position,
                PersonMissionEnumDtoMapper.ToDto(x.EnrollmentKind),
                x.EnrolledAt,
                x.ExcludedAt,
                x.UpdatedAtUtc))
            .ToList();

        return new PagedResult<PersonListItemDto>(items, pageData.Page, pageData.PageSize, pageData.TotalCount);
    }
}
