//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonListItemDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed record PersonListItemDto(
    Guid Id,
    string FullName,
    string Rnokpp,
    PersonLifecycle Lifecycle,
    string? Rank,
    int? PositionSort,
    string? Position,
    EnrollmentKind? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    DateTime UpdatedAtUtc);
