//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// DTO рядка реєстру документів планування (/task-documents).
///
/// Призначення:
/// - швидкий список документів по місяцю з фільтрами (status/search)///
/// </summary>
public sealed record CombatTaskDocumentDto
(
    Guid DocumentId,
    string OrderTitle,
    string? Description,
    DocumentStatusDto Status,
    DateOnly RecordedAt,
    string CanceledReason);