//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicySeed
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public static class TimesheetPolicySeed
{
    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // 1) Codes
        if (!await db.TimesheetCodes.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            const string author = "system";

            var codes = new List<TimesheetCodeDefinition>
            {
                // Системний
                New(TimesheetSystemCodes.NotInTimesheet, "Поза табелем (системний стан, не є подією).", 0, 0),

                // База
                New("Т",    "Базовий стан (служба/перебування у тилу).", 10, 10),
                New("30",   "Базовий “польовий” стан (“день у районі виконання завдань”).",   20, 20),

                // Обставини
                New("ВДР",  "Фіксує період відрядження.",      30, 30),
                New("ВЛК",  "Фіксує період проходження ВЛК.",  40, 40),
                New("МСЕК", "Фіксує період проходження МСЕК.", 41, 41),

                New("ЛХ",   "Період лікування/перебування на лікуванні (хвороба).",   50, 50),
                New("ЛП",   "Період лікування/перебування на лікуванні (поранення).", 51, 51),

                New("ВП",   "Період відпустки (щорічна або сімейна).",  60, 60),
                New("ВПХ",  "Період відпустки (хвороба).",   61, 61),
                New("ВПП",  "Період відпустки (поранення).", 62, 62),

                New("БВ",        "Фіксує період стану “безвісти”.", 70, 70),
                New("П",         "Фіксує період полону.",    71, 71),
                New("А",         "Фіксує період арешту.",    72, 72),
                New("БВ (СЗЧ)",  "Фіксує період безпідставної відсутності.",   73, 73),
                New("СЗЧ",       "Фіксує період СЗЧ.",  74, 74),

                New("РОЗПОР", "Адміністративний стан “розпорядження”.", 80, 80),

                // Фінальне
                New("200", "Загиблий (Фінальний стан).",  99, 99, isTerminal: true),

                // Завдання/факти
                New("100",  "Фіксує факт участі у завданні.", 110, 110),
                New("Ф100", "День отримання поранення/травмування (факт)", 120, 120),
            };

            foreach (var c in codes)
            {
                c.Id = Guid.NewGuid();
                c.CreatedBy = author;
                c.CreatedAtUtc = now;
                c.Code = c.Code.Trim();
                c.Title = c.Title.Trim();
            }

            db.TimesheetCodes.AddRange(codes);
            await db.SaveChangesAsync(ct);
        }

        // 2) Transitions (матриця)
        if (!await db.TimesheetCodeTransitions.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            const string author = "system";

            var map = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Code, x => x.Id, ct);

            var list = new List<TimesheetCodeTransition>();

            void Add(string from, string to, int shift)
            {
                list.Add(new TimesheetCodeTransition
                {
                    Id = Guid.NewGuid(),
                    FromCodeId = map[from],
                    ToCodeId = map[to],
                    StartShiftDays = shift,
                    CreatedBy = author,
                    CreatedAtUtc = now
                });
            }

            void AddFrom(string from, string[]? shift0 = null, string[]? shift1 = null)
            {
                if (shift0 is not null)
                    foreach (var to in shift0) Add(from, to, shift: 0);

                if (shift1 is not null)
                    foreach (var to in shift1) Add(from, to, shift: 1);
            }

            // =================================================================
            // Нижче — повний набір правил з "статуси переходи 3.xlsx"
            // shift 0 = "З дати події"
            // shift 1 = "Ще поточний"
            // =================================================================

            AddFrom("Т",
                shift0: ["30", "ВДР", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("30",
                shift0: ["Т", "ВДР", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "100", "Ф100"]);

            AddFrom("ВДР",
                shift0: ["ЛХ", "ЛП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200"],
                shift1: ["Т", "30"]);

            AddFrom("ВЛК",
                shift0: ["ЛХ", "ЛП", "ВП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"],
                shift1: ["Т", "30"]);

            AddFrom("МСЕК",
                 shift0: ["ЛХ", "ЛП", "ВП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"],
                 shift1: ["Т", "30"]);

            AddFrom("ЛХ",
                shift0: ["ВЛК", "МСЕК", "ЛП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"],
                shift1: ["Т", "30"]);

            AddFrom("ЛП",
                shift0: ["ВЛК", "МСЕК", "ЛП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"],
                shift1: ["Т", "30"]);

            AddFrom("ВП",
                shift0: ["Т", "30", "ЛХ", "ЛП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("ВПХ",
                shift0: ["Т", "30", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("ВПП",
                shift0: ["Т", "30", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("БВ",
                shift0: ["Т", "30", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("П",
                shift0: ["Т", "30", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("А",
               shift0: ["Т", "30", "П", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("БВ (СЗЧ)",
                shift0: ["Т", "30", "БВ", "П", "А", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("СЗЧ",
                 shift0: ["Т", "30", "БВ", "П", "А", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

            AddFrom("РОЗПОР",
                shift0: ["Т", "30"]);

            // 200 (термінальний) — переходів немає за картою

            AddFrom("100",
                shift0: ["БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "200", "Ф100"],
                shift1: ["30"]);

            AddFrom("Ф100",
                shift0: ["ВДР", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПХ", "ВПП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200"],
                shift1: ["Т", "30"]);

            db.TimesheetCodeTransitions.AddRange(list);
            await db.SaveChangesAsync(ct);
        }
    }

    private static TimesheetCodeDefinition New(string code, string title, int sort, int priority, bool isTerminal = false)
        => new()
        {
            Code = code,
            Title = title,
            SortOrder = sort,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = true
        };
}