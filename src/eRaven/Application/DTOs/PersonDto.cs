//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs;

/// <summary>
/// Модель для карточки персони
/// </summary>
public record PersonDto(
    Guid Id,
    string FullName,
    string Rnokpp,
    PersonLifecycle Lifecycle,
    EnrollmentKind? EnrollmentKind,
    string? Rank,
    string? Position,
    string? TemporaryPosition,
    string? PlannedPosition,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    DateTime UpdatedAtUtc,
    string? Bzvp,
    string? Weapon,
    string? Callsign);
