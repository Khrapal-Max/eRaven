//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPersonalInfoUpdated
//-----------------------------------------------------------------------------

using eRaven.Domain.ValueObjects;

namespace eRaven.Domain.Events;

/// <summary>
/// Редагування базового профілю особи (наприклад, зміна прізвища тощо).
/// </summary>
public sealed record PersonPersonalInfoUpdated(
    Guid EventId,
    Guid AggregateId,
    PersonalInfo Personal,
    string? PlannedPosition,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
