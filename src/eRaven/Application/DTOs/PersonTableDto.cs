//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs;

/// <summary>
/// Модель для табличного відображення персони
/// </summary>
public sealed record PersonTableDto(
    Guid Id,
    string FullName,
    string Rnokpp,
    PersonLifecycle Lifecycle,
    EnrollmentKind EnrollmentKind,
    string? Rank,
    string? Position,
    string? TemporaryPosition,
    string? PlannedPosition,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    DateTime UpdatedAtUtc
);