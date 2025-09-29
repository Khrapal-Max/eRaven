//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Domain.Events;

/// <summary>
/// Базовий інтерфейс доменних подій агрегату <see cref="Person"/>.
/// </summary>
public interface IPersonEvent
{
    Guid PersonId { get; }

    /// <summary>
    /// Момент часу (UTC), коли сталася подія.
    /// </summary>
    DateTime OccurredAtUtc { get; }
}
