//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatEntryDetailsDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO "групи" участей у документі планування.
/// Група = один старт (From + SourceDocNo + Mission) та список осіб.
/// Закриття групи відображається через To/EndSourceDocNo.
/// </summary>
public sealed record CombatEntryDetailsDto(
    Guid GroupId,
    string SourceDocNo,
    Guid MissionId,
    string MissionDisplay,
    DateOnly From,
    DateOnly? To,
    string? EndSourceDocNo,
    IReadOnlyList<CombatEntryPersonDto> Persons);
