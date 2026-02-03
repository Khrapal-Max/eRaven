//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StartCombatTaskGroupCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

namespace eRaven.Application.Commands.CombatTask;

/// <summary>
/// Додає групу участей (Start) у документі планування:
/// створює по одному відкритому інтервалу MissionParticipation на кожну обрану особу.
/// </summary>
/// <param name="DocumentId">Ідентифікатор документа.</param>
/// <param name="SourceDocNo">Номер документа-джерела (рапорт/наказ).</param>
/// <param name="MissionId">Ідентифікатор місії.</param>
/// <param name="MissionDisplaySnapshot">Снапшот відображення місії.</param>
/// <param name="From">Дата початку участі (inclusive).</param>
/// <param name="Persons">
/// Обрані особи зі snapshot-полями (RNOKPP/FullName/Rank/Position/Weapon/Callsign).
/// Рекомендація: формувати зі списку “вільні на дату” (picker).
/// </param>
/// <param name="Author">Хто виконав дію.</param>
/// <param name="NowUtc">Час виконання (UTC).</param>
public sealed record StartCombatTaskGroupCommand(
    Guid DocumentId,
    string SourceDocNo,
    Guid MissionId,
    string MissionDisplaySnapshot,
    DateOnly From,
    IReadOnlyCollection<CombatTaskPersonLookupDto> Persons,
    string Author,
    DateTime NowUtc);
