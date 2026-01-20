//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultTimesheetStatusCatalog
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Catalogs.Timesheet;

public sealed class DefaultTimesheetStatusCatalog : ITimesheetStatusCatalog
{
    private static readonly TimesheetStatusOption[] _all =
    [
        // ----------------------------
        // Main lane (основний таймлайн)
        // ----------------------------
        // Статуси наявності
        // служба
        new("Рекрут", TimesheetLane.Main, "0"), // в районі, але не готовий до отримання завдання
        new("В районі", TimesheetLane.Main, "30"), // готовий до отримання завдання
        new("В БР", TimesheetLane.Main, "100"),    // виконує завдання отримане після планування
        new("ПБД", TimesheetLane.Main, "Номер ПБД", IsCodeTemplate: true), // виокнує завдання отримане без планування

        // Статуси відсутності
        // відрядження
        new("Відрядження", TimesheetLane.Main, "ВДР"), // сюди відносимо і БТГр, БТГр це бойове відрядження.

        // відпустки
        new("Відпустка", TimesheetLane.Main, "ВП"),
        new("Відпустка по хворобі", TimesheetLane.Main, "ВПХ"),
        new("Відпустка по пораненню", TimesheetLane.Main, "ВПП"),

        // медицина        
        new("Ф100", TimesheetLane.Main, "Ф100"), // формуляр 100, медогляд по зверненню або пораненню чи травмуванню
        new("Проходження ВЛК", TimesheetLane.Main, "ВЛК"), // військово-лікарська комісія
        new("Лікування по хворобі", TimesheetLane.Main, "ЛХ"),
        new("Лікування по пораненню", TimesheetLane.Main, "ЛП"),

        // “фінальні/інцидентні” (як коди табелю, навіть якщо бізнес-логіка закриття картки окремо)
        new("У розпорядженні", TimesheetLane.Main, "РОЗПОР"),

        new("Безвісти зниклий", TimesheetLane.Main, "БВ"),
        new("Полон", TimesheetLane.Main, "П"),
        new("Загиблий", TimesheetLane.Main, "200"),

        new("Арешт", TimesheetLane.Main, "А"),
        new("Безпідставно відсутній", TimesheetLane.Main, "БВ (СЗЧ)"),
        new("Самовільне залишення частини", TimesheetLane.Main, "СЗЧ"),

        // ----------------------------
        // Task lane (завдання/доручення)
        // ----------------------------
        new("План БР", TimesheetLane.Task, "Номер рапорта", IsCodeTemplate: true) // план на бойове завдання
    ];

    public IReadOnlyList<TimesheetStatusOption> GetAll()
        => _all;

    public IReadOnlyList<TimesheetStatusOption> GetByLane(TimesheetLane lane)
        => [.. _all.Where(x => x.Lane == lane)];
}
