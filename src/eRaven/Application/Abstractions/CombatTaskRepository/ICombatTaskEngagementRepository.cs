//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskEngagementRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Application.Abstractions.CombatTaskRepository;

/// <summary>
/// Репозиторій синхронізації "фактів документа" з інтервалами зайнятості (призначеннями) у домені CombatTask.
///
/// <para>
/// Призначення:
/// <list type="bullet">
/// <item><description>на підставі <see cref="CombatTaskDetails"/> (Start/End) підтримувати таблицю <see cref="MissionAssignment"/>;</description></item>
/// <item><description>гарантувати інваріанти зайнятості через <see cref="PersonTaskEngagementAggregate"/> (1 active task per person);</description></item>
/// <item><description>не торкатися табеля напряму — зовнішній оркестратор може сформувати points для Timesheet із <see cref="MissionAssignment"/> + документів.</description></item>
/// </list>
/// </para>
/// </summary>
public interface ICombatTaskEngagementRepository
{
    /// <summary>
    /// Застосовує факти однієї місії документа (Start/End) і оновлює інтервали зайнятості.
    /// </summary>
    /// <remarks>
    /// Метод є "replace-all" для комбінації (documentId, missionId):
    /// якщо в попередньому стані документ стартував інтервал для особи, але особа більше не присутня у <paramref name="details"/>,
    /// інтервал буде компенсовано (видалено/розкрито) для цієї місії.
    /// </remarks>
    Task<IReadOnlyList<Guid>> ApplyMissionFactsAsync(
        Guid documentId,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Компенсує (Cancel) факти однієї місії документа: видаляє/відкочує інтервали зайнятості.
    /// </summary>
    Task<IReadOnlyList<Guid>> CancelMissionFactsAsync(
        Guid documentId,
        Guid missionId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
