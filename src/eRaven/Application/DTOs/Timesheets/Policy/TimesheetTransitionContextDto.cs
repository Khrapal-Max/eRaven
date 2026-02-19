//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionContextDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets.Policy;

/// <summary>
/// Контекст для UI переходу табеля на конкретну дату:
/// поточний код + список дозволених переходів (опцій).
/// </summary>
/// <param name="PersonId">Особа, для якої формується контекст.</param>
/// <param name="OnDate">Дата, для якої визначено поточний стан.</param>
/// <param name="CurrentCodeId">Id поточного коду на OnDate (Guid.Empty якщо derived "НБ").</param>
/// <param name="CurrentCode">Поточний код (наприклад 30/Т/ЛХ або "НБ").</param>
/// <param name="Options">Дозволені опції переходів для поточного коду.</param>
public sealed record TimesheetTransitionContextDto(
    Guid PersonId,
    DateOnly OnDate,
    Guid CurrentCodeId,
    string CurrentCode,
    IReadOnlyList<TimesheetTransitionOptionDto> Options);
