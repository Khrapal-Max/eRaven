//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonsPageQuery
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.Queries.Personal;

public sealed record GetPersonsPageQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    DateOnly? AsOfDate = null,
    PersonLifecycleDto? Lifecycle = null,
    EnrollmentKindDto? EnrollmentKind = null);
