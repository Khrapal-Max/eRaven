//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQuery
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Queries;

public sealed record GetPersonsPageQuery(
    int Page,
    int PageSize,
    string? Search = null,
    DateOnly? AsOfDate = null,
    PersonLifecycle? Lifecycle = null,
    EnrollmentKind? EnrollmentKind = null
);