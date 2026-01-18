//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries.Personal;

public sealed record GetPersonsPageQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    DateOnly? AsOfDate = null,
    PersonLifecycle? Lifecycle = null,
    EnrollmentKind? EnrollmentKind = null);
