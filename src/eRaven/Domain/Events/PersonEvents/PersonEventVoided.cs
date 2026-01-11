//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonEventVoided
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events.PersonEvents;

/// <summary>
/// Подія анулювання (void) доменної події.
/// Використовується для корекцій без переписування історії.
/// </summary>
public sealed record PersonEventVoided(
    /// <summary>
    /// Унікальний ідентифікатор цієї події
    /// </summary>
    Guid EventId,

    /// <summary>
    /// Ідентифікатор агрегата
    /// </summary>
    Guid AggregateId,

    /// <summary>
    /// Id події, яка вважається недійсною
    /// </summary>
    Guid TargetEventId,

    /// <summary>
    /// Причина корекції
    /// </summary>
    string Reason,

    string Author,
    DateTime OccurredAtUtc
) : IDomainEvent;