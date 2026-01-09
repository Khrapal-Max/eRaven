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
        => evt is IEffectiveDatedEvent dated ? dated.EffectiveDate : null;
}
