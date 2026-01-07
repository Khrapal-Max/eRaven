//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjector (production-clean)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Projectors;

public sealed class PersonReadModelProjector(AppDbContext db) : IPersonReadModelProjector
{
    private readonly AppDbContext _db = db;

    public async Task ProjectAsync(PersonEventRecord record, CancellationToken ct = default)
    {
        var evt = PersonEventTypeRegistry.Deserialize(record);

        // void => повний rebuild
        if (evt is PersonEventVoided)
        {
            await RebuildAsync(record.AggregateId, ct);
            return;
        }

        var rm = await _db.PersonRead.SingleOrDefaultAsync(x => x.Id == record.AggregateId, ct);

        // create
        if (evt is PersonCandidateCreated created)
        {
            if (rm is not null)
                return; // idempotent create

            rm = CreateFrom(created);
            rm.Version = record.Version;
            rm.UpdatedAtUtc = record.OccurredAtUtc;

            _db.PersonRead.Add(rm);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // update
        if (rm is null)
            return;

        if (record.Version <= rm.Version)
            return;

        Apply(rm, evt);

        rm.Version = record.Version;
        rm.UpdatedAtUtc = record.OccurredAtUtc;

        await _db.SaveChangesAsync(ct);
    }

    // ============================
    // Rebuild after void
    // ============================

    private async Task RebuildAsync(Guid aggregateId, CancellationToken ct)
    {
        var records = await _db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == aggregateId)
            .OrderBy(x => x.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
        {
            var rmMissing = await _db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
            if (rmMissing is not null)
            {
                _db.PersonRead.Remove(rmMissing);
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        var events = records.Select(PersonEventTypeRegistry.Deserialize).ToList();

        var voided = new HashSet<Guid>(
            events.OfType<PersonEventVoided>().Select(x => x.TargetEventId)
        );

        var created = events.OfType<PersonCandidateCreated>().FirstOrDefault();
        if (created is null)
        {
            var rmBad = await _db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
            if (rmBad is not null)
            {
                _db.PersonRead.Remove(rmBad);
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        var rebuilt = CreateFrom(created);

        foreach (var evt in events)
        {
            if (evt is PersonEventVoided)
                continue;

            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonCandidateCreated)
                continue;

            Apply(rebuilt, evt);
        }

        var last = records[^1];
        rebuilt.Version = last.Version;
        rebuilt.UpdatedAtUtc = last.OccurredAtUtc;

        var existing = await _db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
        if (existing is null)
        {
            _db.PersonRead.Add(rebuilt);
        }
        else
        {
            CopyTo(existing, rebuilt);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ============================
    // Apply & helpers
    // ============================

    private static PersonReadModel CreateFrom(PersonCandidateCreated created)
        => new()
        {
            Id = created.AggregateId,
            Lifecycle = PersonLifecycle.Candidate,

            Rnokpp = created.Personal.Rnokpp,
            LastName = created.Personal.LastName,
            FirstName = created.Personal.FirstName,
            MiddleName = created.Personal.MiddleName,
            FullName = created.Personal.FullName,
            PlannedPosition = Normalize(created.PlannedPosition),

            // дефолти enrollment
            EnrollmentKind = EnrollmentKind.Unit,
            EnrollmentReference = null,

            // інші поля лишаються null/дефолт
        };

    private static void Apply(PersonReadModel rm, IDomainEvent evt)
    {
        switch (evt)
        {
            case PersonPersonalInfoUpdated x:
                rm.Rnokpp = x.Personal.Rnokpp;
                rm.LastName = x.Personal.LastName;
                rm.FirstName = x.Personal.FirstName;
                rm.MiddleName = x.Personal.MiddleName;
                rm.FullName = x.Personal.FullName;
                rm.PlannedPosition = Normalize(x.PlannedPosition);
                return;

            case PersonRankChanged x:
                rm.Rank = x.Rank;
                return;

            case PersonPositionChanged x:
                rm.Position = x.Position;
                return;

            case PersonTemporaryPositionChanged x:
                rm.TemporaryPosition = Normalize(x.TemporaryPosition);
                return;

            case PersonBzvpChanged x:
                rm.Bzvp = x.Bzvp;
                return;

            case PersonWeaponChanged x:
                rm.Weapon = Normalize(x.Weapon);
                return;

            case PersonCallsignChanged x:
                rm.Callsign = Normalize(x.Callsign);
                return;

            case PersonEnrolled x:
                rm.Lifecycle = PersonLifecycle.Enrolled;
                rm.EnrolledAt = x.EnrollDate;
                rm.EnrollmentKind = x.Kind;
                rm.EnrollmentReference = Normalize(x.Reference);
                return;

            case PersonExcluded x:
                rm.Lifecycle = PersonLifecycle.Excluded;
                rm.ExcludedAt = x.EffectiveDate;
                return;

            default:
                return;
        }
    }

    private static void CopyTo(PersonReadModel target, PersonReadModel source)
    {
        target.Lifecycle = source.Lifecycle;

        // enrollment
        target.EnrollmentKind = source.EnrollmentKind;
        target.EnrollmentReference = source.EnrollmentReference;

        // personal
        target.Rnokpp = source.Rnokpp;
        target.LastName = source.LastName;
        target.FirstName = source.FirstName;
        target.MiddleName = source.MiddleName;
        target.FullName = source.FullName;
        target.PlannedPosition = source.PlannedPosition;

        // key points
        target.Rank = source.Rank;
        target.Position = source.Position;
        target.TemporaryPosition = source.TemporaryPosition;
        target.Bzvp = source.Bzvp;
        target.Weapon = source.Weapon;
        target.Callsign = source.Callsign;

        // dates
        target.EnrolledAt = source.EnrolledAt;
        target.ExcludedAt = source.ExcludedAt;

        // meta
        target.Version = source.Version;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
