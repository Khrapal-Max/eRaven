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

public sealed class CreateCandidateCommandHandler(AppDbContext db, IPersonReadModelProjector projector)
{
    private readonly AppDbContext _db = db;
    private readonly IPersonReadModelProjector _projector = projector;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        // 2) (опційно, але дуже бажано) перевірка дубля РНОКПП по read-model
        var rnokppExists = await _db.PersonRead
            .AsNoTracking()
            .AnyAsync(x => x.Rnokpp == command.Rnokpp, ct);

        if (rnokppExists)
            throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        // 3) create aggregate -> events
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
            author: "author", //TODO : звідки беремо автора?
            nowUtc: DateTime.Now);

        var events = agg.GetUncommittedChanges();
        if (events.Count == 0)
            throw new InvalidOperationException("CreateCandidate не створив подій.");

        // 4) persist events (новий агрегат => Version з 1)
        var records = events
            .Select((e, i) => ToRecord(e, version: i + 1))
            .ToList();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.PersonEvents.AddRange(records);
        await _db.SaveChangesAsync(ct);

        // 5) project read-model (послідовно)
        foreach (var r in records)
            await _projector.ProjectAsync(r, ct);

        await tx.CommitAsync(ct);

        // 6) clear changes
        agg.ClearUncommittedChanges();

        return id;
    }
}