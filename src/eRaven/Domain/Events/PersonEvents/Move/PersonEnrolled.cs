//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEnrolled (updated)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Events.PersonEvents.Move;

/// <summary>
/// Подія зарахування особи у підрозділі.
/// </summary>
public sealed record PersonEnrolled(
    Guid EventId,
    Guid AggregateId,
    EnrollmentKind Kind,
    string? Reference,
    string Reason,
    DateOnly EnrollDate,
    string Position,
    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;
