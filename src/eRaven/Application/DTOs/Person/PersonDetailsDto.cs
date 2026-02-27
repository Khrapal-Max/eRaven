//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed record PersonDetailsDto(
    Guid Id,
    PersonLifecycleDto Lifecycle,
    EnrollmentKindDto? EnrollmentKind,
    string? EnrollmentReference,
    string Rnokpp,
    string LastName,
    string FirstName,
    string? MiddleName,
    string FullName,
    string? Rank,
    int? PositionSort,
    string? Position,
    string? Bzvp,
    string? Weapon,
    string? Callsign,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    long Version,
    DateTime UpdatedAtUtc);
