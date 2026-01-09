//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEnrolled (updated)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Events;

/// <summary>
/// Подія зарахування особи у підрозділі.
/// </summary>
public sealed record PersonEnrolled(
    Guid EventId,
    Guid AggregateId,
    EnrollmentKind Kind,
    string? Reference,          // № наказу / № списку / інша коротка прив’язка (optional)
    string Reason,              // підстава/коментар (обов’язково)
    DateOnly EnrollDate,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent, IEffectiveDatedEvent
{
    public DateOnly EffectiveDate => EnrollDate;
}
