//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonDetailsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;

namespace eRaven.Application.Handlers.Personal;

public sealed class GetPersonDetailsQueryHandler(IPersonRepository repo)
    : IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<PersonDetailsDto?> HandleAsync(GetPersonDetailsQuery query, CancellationToken ct = default)
    {
        var person = await _repo.GetByIdAsync(query.PersonId, ct);

        if (person is null)
            return null;

        return new PersonDetailsDto(
            Id: person.Id,
            Lifecycle: person.Lifecycle,
            EnrollmentKind: person.EnrollmentKind,
            EnrollmentReference: person.EnrollmentReference,
            Rnokpp: person.Rnokpp,
            LastName: person.LastName,
            FirstName: person.FirstName,
            MiddleName: person.MiddleName,
            FullName: person.FullName,
            Rank: person.Rank,
            PositionSort: person.PositionSort,
            Position: person.Position,
            Bzvp: person.Bzvp,
            Weapon: person.Weapon,
            Callsign: person.Callsign,
            EnrolledAt: person.EnrolledAt,
            ExcludedAt: person.ExcludedAt,
            Version: person.Version,
            UpdatedAtUtc: person.UpdatedAtUtc);
    }
}