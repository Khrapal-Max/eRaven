//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public interface IPersonRepository
{
    Task<PersonAggregate?> LoadAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет изменения в ПЕРЕДАННЫЙ DbContext (без создания транзакции).
    /// ДОЛЖЕН вызываться внутри транзакции хендлера, если нужно атомарно с другими таблицами.
    /// Возвращает новый stream version.
    /// </summary>
    Task<long> SaveAsync(AppDbContext db, PersonAggregate agg, long expectedVersion, CancellationToken ct = default);

    /// <summary>
    /// Convenience overload: создаёт DbContext + транзакцию внутри.
    /// Возвращает новый stream version.
    /// </summary>
    Task<long> SaveAsync(PersonAggregate agg, long expectedVersion, CancellationToken ct = default);
}
