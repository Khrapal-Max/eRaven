//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Domain.Aggregates;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Projectors;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Handlers;

public sealed class CreateCandidateCommandHandler(IDbContextFactory<AppDbContext> dbFactory, IPersonEventStore eventStore)
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly IPersonEventStore _eventStore = eventStore;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 2) (опційно, але дуже бажано) перевірка дубля РНОКПП по read-model
        var rnokppExists = await _eventStore.PersonExistsAsync(command.Rnokpp, ct);

        if (rnokppExists)
            throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        // 3) create aggregate -> events
        var id = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var personal = new PersonalInfo(
            rnokpp: command.Rnokpp,
            lastName: command.LastName,
            firstName: command.FirstName,
            middleName: command.MiddleName);

        var agg = PersonAggregate.CreateCandidate(
            id: id,
            personal: personal,
            plannedPosition: command.PlannedPosition,
            author: "author", //TODO : звідки беремо автора?
            nowUtc: nowUtc);

        var events = agg.GetUncommittedChanges();
        if (events.Count == 0)
            throw new InvalidOperationException("CreateCandidate не створив подій.");

        // 4) persist events + project read-model (новий агрегат => Version з 1)
        await _eventStore.PersistAndProjectAsync(events, ct);

        // 5) clear changes
        agg.ClearUncommittedChanges();

        return id;
    }
}
