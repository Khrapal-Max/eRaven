//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocumentDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO деталей документа планування для сторінки-редактора:
/// /planning-documents/{id}
///
/// Призначення:
/// - Відображення "шапки" документа (RecordedAt/PlanningDate/Title/Status).
/// - Вивід списку рядків (Lines) у вигляді таблиці.
/// - Основа для UI дій у Draft:
///   додати/редагувати/видалити рядки, провести (Posted), відмінити (Canceled).
///
/// Важливо:
/// - У статусі Draft документ редагується.
/// - У Posted документ стає "джерелом істини" для CombatTaskAssignment (open/close).
/// - У Canceled документ не повинен впливати на план/звітність.
/// </summary>
public sealed record CombatTaskPlanDocumentDetailsDto(
  Guid DocumentId,
  DateOnly RecordedAt,
  DateOnly PlanningDate,
  string PlanningDocTitle,
  CombatTaskPlanDocumentStatus Status,

  DateTime CreatedAtUtc,
  string CreatedBy,
  DateTime? UpdatedAtUtc,
  string? UpdatedBy,

  string? CanceledReason,
  string? CanceledBy,
  DateTime? CanceledAtUtc,

  IReadOnlyList<CombatTaskPlanLineRowDto> Lines
);
