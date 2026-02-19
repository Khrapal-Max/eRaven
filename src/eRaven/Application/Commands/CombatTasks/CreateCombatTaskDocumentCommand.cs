//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.CombatTasks;

/// <summary>
/// Створення документа бойових завдань.
///
/// <para>
/// Примітка: у спрощеній моделі документ одразу створюється як чинний (Active)
/// і формує факт у табелі.
/// </para>
/// </summary>
public sealed record CreateCombatTaskDocumentCommand(
    string OrderTitle,
    DateOnly RecordedAt,
    string? Description,
    string Author,
    DateTime NowUtc);
