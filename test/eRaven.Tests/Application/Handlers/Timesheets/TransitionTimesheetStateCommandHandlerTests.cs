//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler_SameDayReplace_Tests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Handlers.Timesheets;

/// <summary>
/// Регресійні тести на "same-day replace" після Enroll:
/// Enroll створює дефолтний код "Т" на дату зарахування.
/// Далі оператор ставить "30" на ту ж дату — очікуємо, що "Т" буде замінено на "30" in-place
/// (без створення нового інтервалу), і що зміна реально запишеться в БД.
/// </summary>
public sealed class TransitionTimesheetStateCommandHandlerTests
{

}
