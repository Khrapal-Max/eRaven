//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Domain;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Events;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Projectors;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Application.Handlers;

public sealed class CreateCandidateCommandHandler(IDbContextFactory<AppDbContext> dbFactory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<Guid> HandleAsync(CreatePersonCandidateCommand command, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 2) (опційно, але дуже бажано) перевірка дубля РНОКПП по read-model
        var rnokppExists = await db.PersonRead
            .AsNoTracking()
            .AnyAsync(x => x.Rnokpp == command.Rnokpp, ct);

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

        // 4) persist events (новий агрегат => Version з 1)
        var records = new List<PersonEventRecord>(events.Count);
        for (var i = 0; i < events.Count; i++)
            records.Add(ToRecord(events[i], version: i + 1));

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.PersonEvents.AddRange(records);
        await db.SaveChangesAsync(ct);

        // 5) project read-model (послідовно)
        var projector = new PersonReadModelProjector(db);
        foreach (var r in records)
            await projector.ProjectAsync(r, ct);

        await tx.CommitAsync(ct);

        // 6) clear changes
        agg.ClearUncommittedChanges();

        return id;
    }

    private static PersonEventRecord ToRecord(IDomainEvent evt, long version)
        => new()
        {
            EventId = evt.EventId,
            AggregateId = evt.AggregateId,
            Version = version,
            EventType = evt.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(evt, SerializerOptions),
            Author = evt.Author,
            OccurredAtUtc = evt.OccurredAtUtc,
            EffectiveDate = GetEffectiveDate(evt)
        };

    private static DateOnly? GetEffectiveDate(IDomainEvent evt)
        => evt switch
        {
            PersonRankChanged x => x.EffectiveDate,
            PersonPositionChanged x => x.EffectiveDate,
            PersonTemporaryPositionChanged x => x.EffectiveDate,
            PersonBzvpChanged x => x.EffectiveDate,
            PersonWeaponChanged x => x.EffectiveDate,
            PersonCallsignChanged x => x.EffectiveDate,
            PersonExcluded x => x.EffectiveDate,
            PersonEnrolled x => x.EnrollDate,
            _ => null
        };
}
