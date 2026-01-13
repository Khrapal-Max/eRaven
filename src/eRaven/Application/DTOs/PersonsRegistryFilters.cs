//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistryFilters
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs;

public sealed record PersonsRegistryFilters(
PersonLifecycle? Lifecycle = null,
EnrollmentKind? EnrollmentKind = null
);
