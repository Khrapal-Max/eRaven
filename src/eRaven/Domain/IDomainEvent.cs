//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDomainEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain;

public interface IDomainEvent
{
    /// <summary>
    /// Унікальний ідентифікатор події
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Ідентифікатор агрегата
    /// </summary>
    Guid AggregateId { get; }

    /// <summary>
    /// Автор події
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Час фіксації події (UTC, для аудиту)
    /// </summary>
    DateTime OccurredAtUtc { get; }
}