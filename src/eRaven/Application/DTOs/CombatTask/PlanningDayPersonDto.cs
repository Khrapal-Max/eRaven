//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDayPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO одного запису людини для "План на день".
///
/// Показує:
/// - хто (ПІБ/РНОКПП/посада/позивний)
/// - активний інтервал завдання (StartDate–EndDate)
/// - з якого документа це прийшло (DocumentTitle)
///
/// EndDate може бути null => завдання відкрите (ще не закрите документом End).
/// </summary>
public sealed record PlanningDayPersonDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    string? Callsign,
    DateOnly StartDate,
    DateOnly? EndDate,
    string DocumentTitle);