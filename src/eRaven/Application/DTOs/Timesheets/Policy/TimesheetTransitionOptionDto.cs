//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionOptionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets.Policy;

/// <summary>
/// Опція переходу табеля (для UI): вибираємо за Id коду, відображаємо Code + Display.
/// </summary>
/// <param name="TransitionCodeId">Id коду, який буде застосовано як наступний стан.</param>
/// <param name="Code">Текстовий код (наприклад: 30, ЛХ, Ф100).</param>
/// <param name="Display">Людинозрозуміла назва/заголовок коду.</param>
/// <param name="StartShiftDays">Зсув старту (0 = з дати події, 1 = ще поточний).</param>
public sealed record TimesheetTransitionOptionDto(
    Guid TransitionCodeId,
    string Code,
    string Display,
    int StartShiftDays);
