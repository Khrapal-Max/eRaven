//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CancelCombatTaskDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTask;

/// <summary>
/// Скасовує (Void/Cancel) документ бойових завдань через компенсацію:
/// <list type="bullet">
/// <item><description>позначає документ як <c>Canceled</c> (без фізичного видалення);</description></item>
/// <item><description>скасовує всі повʼязані факти у табелі (TaskSpans) по місіях документа.</description></item>
/// </list>
/// </summary>
public sealed record CancelCombatTaskDocumentCommand(
    Guid DocumentId,
    string? Reason,
    string Author,
    DateTime NowUtc);
