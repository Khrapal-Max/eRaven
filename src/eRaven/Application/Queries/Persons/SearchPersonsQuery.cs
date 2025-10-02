//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchPersonsQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using System.Linq.Expressions;

namespace eRaven.Application.Queries.Persons;

public sealed record SearchPersonsQuery(
    Expression<Func<PersonAggregate, bool>>? Predicate = null
);
