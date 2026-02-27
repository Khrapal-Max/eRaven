//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistryFilters
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed record PersonsRegistryFilters(
    PersonLifecycleDto? Lifecycle = null,
    EnrollmentKindDto? EnrollmentKind = null,
    string? Search = null
);
