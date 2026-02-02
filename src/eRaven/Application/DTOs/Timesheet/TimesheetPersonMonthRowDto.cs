//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonMonthRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// Рядок “табель-місяць” для особи: базові дані + derived-матриця кодів по днях.
/// Використовується у “табель за місяць” та у “табель особи” (для календаря).
/// </summary>
/// <param name="PersonId">Ідентифікатор особи.</param>
/// <param name="FullName">ПІБ (повністю).</param>
/// <param name="RNOKPP">РНОКПП.</param>
/// <param name="Rank">Звання (опційно).</param>
/// <param name="Position">Посада (опційно).</param>
/// <param name="EnrollmentKind">Тип зарахування (опційно).</param>
/// <param name="EnrolledAt">Дата зарахування (опційно).</param>
/// <param name="ExcludedAt">Дата виключення (опційно).</param>
/// <param name="Codes">
/// Коди табеля по днях (1..DaysInMonth) — derived представлення.
/// Елемент [0] відповідає 1-му числу, [1] — 2-му і т.д.
/// </param>
/// <param name="Referenses">
/// Довідкова інформація по днях (tooltip/пояснення), узгоджена індексами з <paramref name="Codes"/>.
/// Використовується переважно для “alert/fact” кодів (наприклад: 100/ПБД/Ф100).
/// </param>
public sealed record TimesheetPersonMonthRowDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    EnrollmentKind? EnrollmentKind,
    DateOnly? EnrolledAt,
    DateOnly? ExcludedAt,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string?> Referenses);
