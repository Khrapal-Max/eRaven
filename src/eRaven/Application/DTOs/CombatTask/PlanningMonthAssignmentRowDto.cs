//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningMonthAssignmentRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO рядка для місячного плану (/planning або інтеграція в "табель планування").
///
/// Це "денормалізований" view рядок, зручний для матриці/таблиці:
/// - дані людини (snapshot)
/// - дані завдання (район/група/тип/режим/мета)
/// - дати Start/End (End може бути null => відкрите)
/// - PlanningDate/PlanningDocTitle (контекст документа планування)
/// - DocumentStatus (щоб UI міг приховати Canceled або по-іншому підсвітити Draft)
///
/// Використання:
/// - Read-репозиторій для швидких вибірок під UI
/// - Зазвичай живиться з CombatTaskAssignment + зв'язок на статус документа
///   (або через проекцію / join).
/// </summary>
public sealed record PlanningMonthAssignmentRowDto(
Guid AssignmentId,
Guid PersonId,
string FullName,
string RNOKPP,
string? Rank,
string? Position,
string? Weapon,
string? Callsign,

DateOnly PlanningDate,
string PlanningDocTitle,

DateOnly StartDate,
DateOnly? EndDate,

string PositionalArea,
string GroupName,
string? AssetType,
string Mode,
string Goal,

bool IsActual,
CombatTaskPlanDocumentStatus DocumentStatus);
