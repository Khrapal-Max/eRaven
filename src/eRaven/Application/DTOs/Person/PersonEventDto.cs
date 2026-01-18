//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Person;

public sealed record PersonEventDto(
    long Version,
    Guid EventId,
    string EventType,
    string PayloadJson,
    string Author,
    DateTime OccurredAtUtc,
    DateOnly? EffectiveDate);