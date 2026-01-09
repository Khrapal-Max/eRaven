//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Domain.Aggregates;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure.Repositories.PersonRepository;

namespace eRaven.Application.Handlers;

public sealed class CreateCandidateCommandHandler(IPersonRepository repo)
    : ICommandHandler<CreatePersonCandidateCommand, Guid>
{
    private readonly IPersonRepository _repo = repo;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();

        var personal = new PersonalInfo(
            rnokpp: command.Rnokpp,
            lastName: command.LastName,
            firstName: command.FirstName,
            middleName: command.MiddleName);

        var agg = PersonAggregate.CreateCandidate(
            id: id,
            personal: personal,
            plannedPosition: command.PlannedPosition,
            author: "author",
            nowUtc: DateTime.UtcNow);

        await _repo.SaveAsync(agg, expectedVersion: 0, ct);
        return id;
    }
}