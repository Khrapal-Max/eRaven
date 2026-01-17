//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventTypeRegistryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Events.PersonEvents.Info;
using eRaven.Infrastructure.Projectors;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Tests.Infrastructure.Projectors;

public sealed class PersonEventTypeRegistryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Deserialize_should_return_typed_domain_event()
    {
        var evt = new PersonRankChanged(
            EventId: Guid.NewGuid(),
            AggregateId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 1, 10),
            Rank: "Солдат",
            Note: "n",
            Author: "tester",
            OccurredAtUtc: DateTime.UtcNow);

        var record = new PersonEventRecord
        {
            EventId = evt.EventId,
            AggregateId = evt.AggregateId,
            Version = 1,
            EventType = nameof(PersonRankChanged),
            PayloadJson = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions),
            EffectiveDate = evt.EffectiveDate,
            OccurredAtUtc = evt.OccurredAtUtc,
            Author = evt.Author
        };

        var des = PersonEventTypeRegistry.Deserialize(record);

        var typed = Assert.IsType<PersonRankChanged>(des);
        Assert.Equal(evt.EventId, typed.EventId);
        Assert.Equal(evt.AggregateId, typed.AggregateId);
        Assert.Equal(evt.EffectiveDate, typed.EffectiveDate);
        Assert.Equal("Солдат", typed.Rank);
    }

    [Fact]
    public void Deserialize_unknown_event_type_should_throw()
    {
        var record = new PersonEventRecord
        {
            EventId = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            Version = 1,
            EventType = "UnknownEvent",
            PayloadJson = "{}",
            EffectiveDate = null,
            OccurredAtUtc = DateTime.UtcNow,
            Author = "tester"
        };

        Assert.Throws<InvalidOperationException>(() => PersonEventTypeRegistry.Deserialize(record));
    }
}
