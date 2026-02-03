//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionParticipationRowDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Рядок звіту участі в місії (на дату або за період).
/// Дані беруться з MissionParticipation snapshot (без JOIN-ів).
/// </summary>
public sealed record MissionParticipationRowDto(
    Guid DocumentId,
    Guid GroupId,
    int GroupSequence,

    Guid MissionId,
    string MissionDisplaySnapshot,

    Guid PersonId,
    string RNOKPP,
    string FullName,
    string Rank,
    string Position,
    string Weapon,
    string Callsign,

    DateOnly From,
    DateOnly? To,
    string SourceDocNo,
    string? EndSourceDocNo
);
