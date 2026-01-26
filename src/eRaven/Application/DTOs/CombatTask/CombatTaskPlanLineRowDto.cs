//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanLineRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO рядка документа планування для відображення у редакторі документа
/// (/planning-documents/{id}) та для переходу у Line editor:
/// - /planning-documents/{id}/line/new
/// - /planning-documents/{id}/line/{lineId}
///
/// Призначення:
/// - Показати в таблиці "хто / що / коли" (Start або End).
/// - Дати UI мінімальний набір даних для редагування або видалення.
/// - Містить snapshot особи та деталі завдання, щоб документ був стабільний в часі.
///
/// Семантика:
/// - Kind=Start: ActionDate = дата початку виконання, AssignmentId = новий ланцюжок.
/// - Kind=End:   ActionDate = дата закриття, AssignmentId = який ланцюжок закриваємо.
/// </summary>
public sealed record CombatTaskPlanLineRowDto(
    Guid LineId,
    CombatTaskPlanLineKind Kind,

    Guid PersonId,
    Guid AssignmentId,
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
    string? Note
);
