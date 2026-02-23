//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandlerTests
//-----------------------------------------------------------------------------

using eRaven.Application.Handlers.CombatTasks;

namespace eRaven.Tests.Application.Handlers.CombatTasks;

/// <summary>
/// Тести для <see cref="CreateCombatTaskCommandHandler"/>.
///
/// <para>Фіксуємо ключову бізнес-поведінку:</para>
/// <list type="bullet">
/// <item><description>якщо місії в документі немає — викликаємо <c>CreateCombatTaskAsync</c>;</description></item>
/// <item><description>якщо місія вже є — викликаємо <c>UpsertCombatTaskAsync</c> (replace-all rows);</description></item>
/// <item><description>enrich snapshot з існуючих рядків місії (fallback), без зайвих запитів у PersonRepo;</description></item>
/// <item><description>завжди викликаємо <c>ApplyCombatTaskFactsAsync</c> по “current truth” (incoming rows);</description></item>
/// <item><description>trim для <c>SourceDocument</c>/<c>Author</c>/<c>Rnokpp</c>/<c>FullName</c>;</description></item>
/// <item><description>валідації: пустий/NULL список рядків, null-рядок, пусті ключові поля.</description></item>
/// </list>
/// </summary>
public sealed class CreateCombatTaskCommandHandlerTests
{

}
