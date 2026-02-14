//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonEntryRowDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// Запис подій, що відбувалися з працівником у певний період часу. 
/// Використовується для відображення інформації про статус та період його дії
/// </summary>
public sealed record TimesheetPersonEntryRowDto(
string Code,
DateOnly From,
DateOnly? To,
string? Reference,
string? Note);
