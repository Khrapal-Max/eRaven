//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventRecordFactory
//-----------------------------------------------------------------------------

using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Events;
using System.Text.Json;

namespace eRaven.Infrastructure.Projectors;

public sealed class PersonEventRecordFactory
{
    public PersonEventRecord Create(IDomainEvent evt, long version)
        => new()
        {
            EventId = evt.EventId,
            AggregateId = evt.AggregateId,
            Version = version,
            EventType = evt.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(evt, EventJsonOptions.Options),
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
