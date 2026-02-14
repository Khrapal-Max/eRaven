//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetSystemCodes
//-----------------------------------------------------------------------------

namespace eRaven.Infrastructure;

/// <summary>
/// Консатнти для системних кодів табеля, які не є подіями, 
/// а використовуються для внутрішньої логіки та обчислень.
/// </summary>
public static class TimesheetSystemCodes
{
    // Системний (не подія)
    public const string NotInTimesheet = "НБ";

    // База
    public const string BaseState = "Т";
    public const string ReadyToCombatTask = "30";

    // Обставини
    public const string BusinessTrip = "ВДР";
    public const string Vlk = "ВЛК";
    public const string Msek = "МСЕК";

    public const string TreatmentSickness = "ЛХ";
    public const string TreatmentWound = "ЛП";

    public const string Leave = "ВП";
    public const string LeaveSickness = "ВПХ";
    public const string LeaveWound = "ВПП";

    public const string Missing = "БВ";
    public const string Captivity = "П";
    public const string Arrest = "А";
    public const string MissingSzcz = "БВ (СЗЧ)";
    public const string Szcz = "СЗЧ";

    public const string Rozpor = "РОЗПОР";

    // Фінальне
    public const string Killed = "200";

    // Завдання/факти
    public const string DoesTheCombatTask = "100";
    public const string InjuryFact = "Ф100";
}