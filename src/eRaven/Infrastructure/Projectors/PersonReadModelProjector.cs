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

public sealed class PersonReadModelProjector : IPersonReadModelProjector
{
    public async Task ProjectAsync(AppDbContext db, PersonEventRecord record, CancellationToken ct = default)
    {
        var evt = PersonEventTypeRegistry.Deserialize(record);

        if (evt is PersonEventVoided)
        {
            await RebuildAsync(db, record.AggregateId, ct);
            return;
        }

        var rm = await db.PersonRead.SingleOrDefaultAsync(x => x.Id == record.AggregateId, ct);

        if (evt is PersonCandidateCreated created)
        {
            if (rm is not null)
                return; // idempotent create

            rm = CreateFrom(created);
            rm.Version = record.Version;
            rm.UpdatedAtUtc = record.OccurredAtUtc;

            db.PersonRead.Add(rm);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (rm is null)
            return;

        if (record.Version <= rm.Version)
            return;

        Apply(rm, evt);

        rm.Version = record.Version;
        rm.UpdatedAtUtc = record.OccurredAtUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task RebuildAsync(AppDbContext db, Guid aggregateId, CancellationToken ct)
    {
        var records = await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == aggregateId)
            .OrderBy(x => x.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
        {
            var rmMissing = await db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
            if (rmMissing is not null)
            {
                db.PersonRead.Remove(rmMissing);
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var events = records.Select(PersonEventTypeRegistry.Deserialize).ToList();

        // тот же правильный "void-of-void" подход: backward scan
        var voided = new HashSet<Guid>();
        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i];
            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided v)
                voided.Add(v.TargetEventId);
        }

        var created = events.OfType<PersonCandidateCreated>().FirstOrDefault(x => !voided.Contains(x.EventId));
        if (created is null)
        {
            var rmBad = await db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
            if (rmBad is not null)
            {
                db.PersonRead.Remove(rmBad);
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var rebuilt = CreateFrom(created);

        foreach (var evt in events)
        {
            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided)
                continue;

            if (evt is PersonCandidateCreated)
                continue;

            Apply(rebuilt, evt);
        }

        var last = records[^1];
        rebuilt.Version = last.Version;
        rebuilt.UpdatedAtUtc = last.OccurredAtUtc;

        var existing = await db.PersonRead.SingleOrDefaultAsync(x => x.Id == aggregateId, ct);
        if (existing is null)
            db.PersonRead.Add(rebuilt);
        else
            CopyTo(existing, rebuilt);

        await db.SaveChangesAsync(ct);
    }

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

            PlannedPositionUnitId = created.PlannedPositionUnitId,
            PlannedPosition = Normalize(created.PlannedPosition),

            PositionUnitId = null,
            TemporaryPositionUnitId = null,

            // если тебе не нужен дефолт — сделай null
            EnrollmentKind = EnrollmentKind.Unit,
            EnrollmentReference = null,
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
                return;

            case PersonRankChanged x:
                rm.Rank = x.Rank;
                return;

            case PersonPositionChanged x:
                rm.PositionUnitId = x.PositionUnitId;
                rm.Position = x.Position;
                return;

            case PersonTemporaryPositionChanged x:
                rm.TemporaryPositionUnitId = x.TemporaryPositionUnitId;
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
                rm.PlannedPositionUnitId = null;
                rm.PlannedPosition = null;
                rm.PositionUnitId = x.PositionUnitId;
                rm.Position = x.Position;
                return;

            case PersonExcluded x:
                rm.Lifecycle = PersonLifecycle.Excluded;
                rm.ExcludedAt = x.EffectiveDate;
                rm.PlannedPositionUnitId = null;
                rm.PlannedPosition = null;
                rm.PositionUnitId = null;
                rm.Position = null;
                rm.TemporaryPositionUnitId = null;
                rm.TemporaryPosition = null;
                return;
        }
    }

    private static void CopyTo(PersonReadModel target, PersonReadModel source)
    {
        target.Lifecycle = source.Lifecycle;

        target.EnrollmentKind = source.EnrollmentKind;
        target.EnrollmentReference = source.EnrollmentReference;

        target.Rnokpp = source.Rnokpp;
        target.LastName = source.LastName;
        target.FirstName = source.FirstName;
        target.MiddleName = source.MiddleName;
        target.FullName = source.FullName;

        target.PlannedPositionUnitId = source.PlannedPositionUnitId;
        target.PlannedPosition = source.PlannedPosition;

        target.Rank = source.Rank;

        target.PositionUnitId = source.PositionUnitId;
        target.Position = source.Position;

        target.TemporaryPositionUnitId = source.TemporaryPositionUnitId;
        target.TemporaryPosition = source.TemporaryPosition;

        target.Bzvp = source.Bzvp;
        target.Weapon = source.Weapon;
        target.Callsign = source.Callsign;

        target.EnrolledAt = source.EnrolledAt;
        target.ExcludedAt = source.ExcludedAt;

        target.Version = source.Version;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }


    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
