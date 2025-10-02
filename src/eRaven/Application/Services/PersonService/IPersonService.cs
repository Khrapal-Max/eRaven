//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

using eRaven.Domain.Person;
using System.Linq.Expressions;

namespace eRaven.Application.Services.PersonService;

public interface IPersonService
{
    /// <summary>
    /// Повертає список осіб, що відповідають критерію пошуку.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList Person(<see cref="Person"/>)</returns>
    Task<IReadOnlyList<Person>> SearchAsync(Expression<Func<Person, bool>>? predicate, CancellationToken ct = default); // null -> всі

    /// <summary>
    /// Повертає особу за її унікальним ідентифікатором.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns>Person?(<see cref="Person"/>)</returns>
    Task<Person?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Створює нову особу.
    /// </summary>
    /// <param name="person"></param>
    /// <param name="ct"></param>
    /// <returns>Person(<see cref="Person"/>)</returns>
    Task<Person> CreateAsync(Person person, CancellationToken ct = default);

    /// <summary>
    /// Оновлює інформацію про особу.
    /// </summary>
    /// <param name="person"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> UpdateAsync(Person person, CancellationToken ct = default);
}
