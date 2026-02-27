//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonListItemDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed record PersonListItemDto(
    Guid Id,
    string FullName,
    string Rnokpp,
    PersonLifecycleDto Lifecycle,
    string? Rank,
    int? PositionSort,
    string? Position,
    EnrollmentKindDto? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    DateTime UpdatedAtUtc);
