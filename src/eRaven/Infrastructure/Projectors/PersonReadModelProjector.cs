//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonReadModelProjector (updated for simplified lifecycle + text Position)
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Domain.Events.PersonEvents.Move;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Projectors;

public sealed class PersonReadModelProjector : IPersonReadModelProjector
{
    public async Task ProjectAsync(AppDbContext db, PersonEventRecord record, CancellationToken ct = default)
    {
        var evt = PersonEventTypeRegistry.Deserialize(record);

        // void => rebuild whole aggregate
        if (evt is PersonEventVoided)
        {
            await RebuildAsync(db, record.AggregateId, ct);
            return;
        }

        // tracked-safe
        var rm = await db.PersonRead.FindAsync([record.AggregateId], ct);

        if (evt is PersonCreated created)
        {
            if (rm is not null)
                return; // idempotent create

            rm = CreateFrom(created);
            rm.Version = record.Version;
            rm.UpdatedAtUtc = record.OccurredAtUtc;

            db.PersonRead.Add(rm);
            return;
        }

        if (rm is null)
            return;

        // idempotency / monotonic
        if (record.Version <= rm.Version)
            return;

        Apply(rm, evt);

        rm.Version = record.Version;
        rm.UpdatedAtUtc = record.OccurredAtUtc;
    }

    public async Task RebuildAsync(AppDbContext db, Guid aggregateId, CancellationToken ct = default)
    {
        var records = await db.PersonEvents
            .AsNoTracking()
            .Where(x => x.AggregateId == aggregateId)
            .OrderBy(x => x.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
        {
            var rmMissing = await db.PersonRead.FindAsync([aggregateId], ct);
            if (rmMissing is not null)
                db.PersonRead.Remove(rmMissing);

            return;
        }

        var events = records.Select(PersonEventTypeRegistry.Deserialize).ToList();

        // build final voided set (supports "void of void")
        var voided = new HashSet<Guid>();
        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i];

            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided v)
                voided.Add(v.TargetEventId);
        }

        // find first alive PersonCreated
        var created = events
            .OfType<PersonCreated>()
            .FirstOrDefault(x => !voided.Contains(x.EventId));

        if (created is null)
        {
            var rmBad = await db.PersonRead.FindAsync([aggregateId], ct);
            if (rmBad is not null)
                db.PersonRead.Remove(rmBad);

            return;
        }

        var rebuilt = CreateFrom(created);

        foreach (var evt in events)
        {
            if (voided.Contains(evt.EventId))
                continue;

            if (evt is PersonEventVoided)
                continue;

            if (evt is PersonCreated)
                continue;

            Apply(rebuilt, evt);
        }

        var last = records[^1];
        rebuilt.Version = last.Version;
        rebuilt.UpdatedAtUtc = last.OccurredAtUtc;

        var existing = await db.PersonRead.FindAsync([aggregateId], ct);
        if (existing is null)
            db.PersonRead.Add(rebuilt);
        else
            CopyTo(existing, rebuilt);
    }

    private static PersonReadModel CreateFrom(PersonCreated created)
        => new()
        {
            Id = created.AggregateId,
            Lifecycle = PersonLifecycle.Reserved,

            // enrollment initially empty
            EnrollmentKind = null,
            EnrollmentReference = null,

            // personal
            Rnokpp = created.Personal.Rnokpp,
            LastName = created.Personal.LastName,
            FirstName = created.Personal.FirstName,
            MiddleName = created.Personal.MiddleName,
            FullName = created.Personal.FullName,

            // professional / military (можуть бути заповнені вже на створенні)
            Rank = Normalize(created.Rank),
            Position = Normalize(created.Position),
            Bzvp = Normalize(created.Bzvp),
            Weapon = Normalize(created.Weapon),
            Callsign = Normalize(created.Callsign),

            EnrolledAt = null,
            ExcludedAt = null
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
                rm.Position = Normalize(x.Position);
                return;

            case PersonBzvpChanged x:
                rm.Bzvp = Normalize(x.Bzvp);
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

                // звання та посада при enroll обовʼязкова (string)
                rm.Rank = x.Rank.Trim();
                rm.Position = x.Position.Trim();

                // ✅ повторний enroll: очищаємо дату виключення
                rm.ExcludedAt = null;
                return;

            case PersonExcluded x:
                // ✅ виключення повертає у Reserved
                rm.Lifecycle = PersonLifecycle.Reserved;

                // ✅ зберігаємо "останнє виключення" до наступного enroll
                rm.ExcludedAt = x.EffectiveDate;

                // інші поля НЕ чистимо (історія/останні значення лишаються)
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

        target.Rank = source.Rank;
        target.Position = source.Position;

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
