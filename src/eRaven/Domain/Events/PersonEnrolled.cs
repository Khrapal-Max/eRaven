//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEnrolled
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Подія зарахування особи
/// </summary>
public sealed record PersonEnrolled(
    Guid AggregateId,
    string Reason,
    DateOnly EnrollDate,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
