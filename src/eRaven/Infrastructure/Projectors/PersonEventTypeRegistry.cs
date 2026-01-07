//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventTypeRegistry
//-----------------------------------------------------------------------------


using eRaven.Domain;
using eRaven.Domain.Entities;
using eRaven.Domain.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Infrastructure.Projectors;

public static class PersonEventTypeRegistry
{
    private static readonly Dictionary<string, Type> _map = new()
    {
        { nameof(PersonCandidateCreated), typeof(PersonCandidateCreated) },
        { nameof(PersonPersonalInfoUpdated), typeof(PersonPersonalInfoUpdated) },
        { nameof(PersonRankChanged), typeof(PersonRankChanged) },
        { nameof(PersonPositionChanged), typeof(PersonPositionChanged) },
        { nameof(PersonTemporaryPositionChanged), typeof(PersonTemporaryPositionChanged) },
        { nameof(PersonBzvpChanged), typeof(PersonBzvpChanged) },
        { nameof(PersonWeaponChanged), typeof(PersonWeaponChanged) },
        { nameof(PersonCallsignChanged), typeof(PersonCallsignChanged) },
        { nameof(PersonEnrolled), typeof(PersonEnrolled) },
        { nameof(PersonExcluded), typeof(PersonExcluded) },
        { nameof(PersonEventVoided), typeof(PersonEventVoided) },
    };

    public static IDomainEvent Deserialize(PersonEventRecord r)
    {
        if (!_map.TryGetValue(r.EventType, out var type))
            throw new InvalidOperationException($"Unknown event type: {r.EventType}");

        return (IDomainEvent)JsonSerializer.Deserialize(r.PayloadJson, type, _options)!;
    }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
