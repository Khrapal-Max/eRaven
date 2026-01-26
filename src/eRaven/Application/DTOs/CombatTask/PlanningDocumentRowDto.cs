//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDocumentRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO рядка реєстру документів планування (/planning-documents).
///
/// Призначення:
/// - швидкий список документів по місяцю з фільтрами (status/search)
/// - агрегована статистика по рядках:
///   PersonsCount, MinStart, MaxEnd
///
/// Важливо:
/// - DocumentDate: зазвичай це PlanningDate (на який день/період план),
///   якщо у тебе інша семантика — можеш відобразити RecordedAt, але назва поля вже "DocumentDate".
/// - MinStart/MaxEnd обчислюються по Lines/Assignments (залежить від read-repo реалізації).
/// </summary>
public sealed record PlanningDocumentRowDto(
    Guid DocumentId,
    DateOnly DocumentDate,
    string Title,
    CombatTaskPlanDocumentStatus Status,
    int PersonsCount,
    DateOnly? MinStart,
    DateOnly? MaxEnd,
    DateTime CreatedAtUtc
);