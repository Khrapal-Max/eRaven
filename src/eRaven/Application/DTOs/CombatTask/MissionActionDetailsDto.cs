//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionActionDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO змісту місії в документі ланування.
///
/// Призначення:
/// - деталі місії документа.
/// - швидкий список осіб в місії.
/// </summary>
public sealed record MissionActionDetailsDto(
    Guid ActionId,
    int Sequence,
    string SourceDocNo,
    ActionKind Action,
    Guid MissionId,
    string MissionDisplay,
    DateOnly ActionDate,
    IReadOnlyList<MissionActionPersonDto> Persons);
