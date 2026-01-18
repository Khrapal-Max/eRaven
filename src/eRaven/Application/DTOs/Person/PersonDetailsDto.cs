//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Person;

public sealed record PersonDetailsDto(
    Guid Id,
    PersonLifecycle Lifecycle,
    EnrollmentKind? EnrollmentKind,
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
