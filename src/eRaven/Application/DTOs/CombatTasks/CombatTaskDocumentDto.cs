//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// DTO рядка реєстру документів планування (/planning-documents).
///
/// Призначення:
/// - швидкий список документів по місяцю з фільтрами (status/search)///
/// </summary>
public sealed record CombatTaskDocumentDto
(
    Guid DocumentId,
    string OrderTitle,
    string? Description,
    DocumentStatus Status,
    DateOnly RecordedAt,
    string CanceledReason
);
