//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanLineInputDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Input DTO для додавання/редагування рядка планового документа (Draft).
///
/// Використання:
/// - Подається з UI (line editor) у команду створення/оновлення рядка.
/// - Містить "snapshot" особи та деталі завдання, щоб документ не ламався
///   при змінах PersonRead у майбутньому.
///
/// Важливо:
/// - <see cref="Kind"/> визначає семантику <see cref="ActionDate"/>:
///   Start = дата початку виконання, End = дата закриття.
/// - Для End <see cref="AssignmentId"/> може бути null:
///   тоді сервер може "автоматично" знайти відкрите завдання по PersonId.
/// </summary>
public sealed record CombatTaskPlanLineInputDto(
    CombatTaskPlanLineKind Kind,
    Guid PersonId,
    DateOnly ActionDate,

    // Person snapshot
    string RNOKPP,
    string FullName,
    string? Rank,
    string? Position,
    string? Weapon,
    string? Callsign,

    // Task details
    string PositionalArea,
    string GroupName,
    string? AssetType,
    CombatTaskMode Mode,
    string Goal,
    bool IsActual,
    string? Note,

    // For End: optional (can be empty => auto resolve open assignment)
    Guid? AssignmentId);