//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDayGroupDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO для сторінки "План на день" (групований вигляд).
///
/// Групування виконується за ключем:
/// PositionalArea + GroupName + AssetType + Mode + Goal
///
/// Усередині групи міститься список осіб <see cref="PlanningDayPersonDto"/>.
/// Зазвичай джерело: "posted" (або draft+posted — за правилом UI).
/// </summary>
public sealed record PlanningDayGroupDto(
    string PositionalArea,
    string GroupName,
    string? AssetType,
    string Mode,
    string Goal,
    IReadOnlyList<PlanningDayPersonDto> Persons
);