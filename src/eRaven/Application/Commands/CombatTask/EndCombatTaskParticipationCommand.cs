//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EndCombatTaskParticipationCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

/// <summary>
/// Закриває групу участей у документі (End):
/// виставляє To та EndSourceDocNo для всіх відкритих рядків цієї групи.
/// </summary>
/// <param name="DocumentId">Ідентифікатор документа.</param>
/// <param name="GroupId">Ідентифікатор групи.</param>
/// <param name="To">Дата завершення (inclusive).</param>
/// <param name="EndSourceDocNo">Номер документа-джерела завершення.</param>
/// <param name="Author">Хто виконав дію.</param>
/// <param name="NowUtc">Час виконання (UTC).</param>
public sealed record EndCombatTaskGroupCommand(
    Guid DocumentId,
    Guid GroupId,
    DateOnly To,
    string EndSourceDocNo,
    string Author,
    DateTime NowUtc);