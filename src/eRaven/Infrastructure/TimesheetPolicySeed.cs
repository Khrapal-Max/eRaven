//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicySeed
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

public sealed class TimesheetPolicySeed
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct)
    {
        // 1) Codes (НБ НЕ є кодом події табеля, тому НЕ сідаємо його в довідник)
        if (!await db.TimesheetCodes.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            const string author = "seed";

            var codes = new List<TimesheetCodeDefinition>
            {
                // ----------------------------
                // MAIN (факт)
                // ----------------------------

                // базовий факт-стан
                CodeMain("30", "В районі", 10, TimesheetEndDateMeaning.LastDayOfThisCode),

                // “день повернення це ще подія”
                CodeMain("ВДР", "Відрядження", 20, TimesheetEndDateMeaning.LastDayOfThisCode),
                CodeMain("ВЛК", "Проходження ВЛК", 40, TimesheetEndDateMeaning.LastDayOfThisCode),
                CodeMain("МСЕК", "Проходження МСЕК", 41, TimesheetEndDateMeaning.LastDayOfThisCode),
                CodeMain("ЛХ", "Лікування по хворобі", 50, TimesheetEndDateMeaning.LastDayOfThisCode),
                CodeMain("ЛП", "Лікування по пораненню", 51, TimesheetEndDateMeaning.LastDayOfThisCode),

                // “день повернення це наступна подія” (30 з цієї дати)
                CodeMain("ВП", "Відпустка", 30, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),
                CodeMain("ВПХ", "Відпустка по хворобі", 31, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),
                CodeMain("ВПП", "Відпустка по пораненню", 32, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),

                // інцидентні (вихід потім у 30/РОЗПОР, але НЕ в НБ)
                CodeMain("БВ", "Безвісти", 60, TimesheetEndDateMeaning.FirstDayOfNextCode),
                CodeMain("П", "Полон", 61, TimesheetEndDateMeaning.FirstDayOfNextCode),
                CodeMain("А", "Арешт", 62, TimesheetEndDateMeaning.FirstDayOfNextCode),
                CodeMain("БВ (СЗЧ)", "Безпідставно відсутній", 63, TimesheetEndDateMeaning.FirstDayOfNextCode),
                CodeMain("СЗЧ", "СЗЧ", 64, TimesheetEndDateMeaning.FirstDayOfNextCode),

                // адмін/фінальні
                CodeMain("РОЗПОР", "Розпорядження", 80, TimesheetEndDateMeaning.FirstDayOfNextCode, isTerminal: true),
                CodeMain("200", "Загибель", 99, TimesheetEndDateMeaning.LastDayOfThisCode, isTerminal: true),

                // ----------------------------
                // MAIN: завдання/факти, що впливають на план
                // ----------------------------
                CodeMain("100", "Затверджене завдання (факт)", 110, TimesheetEndDateMeaning.LastDayOfThisCode),

                CodeMain("Ф100", "Ф100 (факт поранення)", 120, TimesheetEndDateMeaning.LastDayOfThisCode,
                    isPlanningCutoff: true, cutoffShiftDays: 1),

                CodeMain("ПБД", "ПБД (факт)", 130, TimesheetEndDateMeaning.LastDayOfThisCode,
                    requiresReference: true,
                    isPlanningCutoff: true, cutoffShiftDays: 1),

                // ----------------------------
                // TASK (план) — мінімально
                // ----------------------------
                new() {
                    Lane = TimesheetLane.Task,
                    Code = "PLAN",
                    Title = "План (чернетка)",
                    SortOrder = 10,
                    RequiresReference = true,
                    EndDateMeaning = TimesheetEndDateMeaning.LastDayOfThisCode
                } // TODO додати інші коди плану за потреби
            };

            foreach (var c in codes)
            {
                c.Id = Guid.NewGuid();
                c.CreatedBy = author;
                c.CreatedAtUtc = now;
            }

            db.TimesheetCodes.AddRange(codes);
            await db.SaveChangesAsync(ct);
        }

        // 2) Transitions (без НБ, без -> НБ)
        if (!await db.TimesheetCodeTransitions.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            const string author = "seed";

            var main = await db.TimesheetCodes
                .Where(x => x.Lane == TimesheetLane.Main && x.IsActive)
                .ToListAsync(ct);

            var task = await db.TimesheetCodes
                .Where(x => x.Lane == TimesheetLane.Task && x.IsActive)
                .ToListAsync(ct);

            var mainByCode = main.ToDictionary(x => x.Code, x => x.Id);
            var taskByCode = task.ToDictionary(x => x.Code, x => x.Id);

            void AddMain(string fromCode, params string[] toCodes)
                => AddAll(db, TimesheetLane.Main, mainByCode[fromCode], toCodes.Select(c => mainByCode[c]), author, now);

            // 30 -> все (крім себе)
            var id30 = mainByCode["30"];
            AddAll(db, TimesheetLane.Main, id30, main.Select(x => x.Id).Where(x => x != id30), author, now);

            // ВДР дозволені: 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ)
            AddMain("ВДР", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)");

            // ВП: 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ)
            AddMain("ВП", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)");

            // ВПХ: 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ), ВЛК
            AddMain("ВПХ", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК");

            // ВПП: 30, ЛП, СЗЧ, 200, А, БВ (СЗЧ), ВЛК
            AddMain("ВПП", "30", "ЛП", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК");

            // ВЛК: 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ), ЛП, ВПХ, ВПП
            AddMain("ВЛК", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ЛП", "ВПХ", "ВПП");

            // МСЕК: 30
            AddMain("МСЕК", "30");

            // ЛХ: 30, ВПХ, СЗЧ, 200, А, БВ (СЗЧ), ВЛК, РОЗПОР
            AddMain("ЛХ", "30", "ВПХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК", "РОЗПОР");

            // ЛП: 30, ВПП, СЗЧ, 200, А, БВ (СЗЧ), ВЛК, РОЗПОР
            AddMain("ЛП", "30", "ВПП", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК", "РОЗПОР");

            // БВ: 30, П, СЗЧ, 200, БВ (СЗЧ), РОЗПОР
            AddMain("БВ", "30", "П", "СЗЧ", "200", "БВ (СЗЧ)", "РОЗПОР");

            // П: 30, РОЗПОР
            AddMain("П", "30", "РОЗПОР");

            // А: 30, РОЗПОР
            AddMain("А", "30", "РОЗПОР");

            // БВ (СЗЧ): 30, РОЗПОР, СЗЧ, 200
            AddMain("БВ (СЗЧ)", "30", "РОЗПОР", "СЗЧ", "200");

            // СЗЧ: 30, РОЗПОР
            AddMain("СЗЧ", "30", "РОЗПОР");

            // РОЗПОР: 30
            AddMain("РОЗПОР", "30");

            // 200: переходів немає (термінальний факт), НБ НЕ використовуємо

            // 100: 30, БВ, ПБД, 200, БВ (СЗЧ), П, Ф100
            AddMain("100", "30", "БВ", "ПБД", "200", "БВ (СЗЧ)", "П", "Ф100");

            // Ф100: 30, ЛП, ЛХ
            AddMain("Ф100", "30", "ЛП", "ЛХ");

            // ПБД: 30, 100
            AddMain("ПБД", "30", "100");

            // TASK: якщо колись буде >1 коду — дозволимо між ними
            foreach (var from in taskByCode.Values)
                AddAll(db, TimesheetLane.Task, from, taskByCode.Values.Where(x => x != from), author, now);

            await db.SaveChangesAsync(ct);
        }

        static TimesheetCodeDefinition CodeMain(
            string code,
            string title,
            int sort,
            TimesheetEndDateMeaning endMeaning,
            string? nextCodeOnEnd = null,
            bool isTerminal = false,
            bool requiresReference = false,
            bool isPlanningCutoff = false,
            int cutoffShiftDays = 1)
            => new()
            {
                Lane = TimesheetLane.Main,
                Code = code,
                Title = title,
                SortOrder = sort,
                IsTerminal = isTerminal,
                RequiresReference = requiresReference,
                EndDateMeaning = endMeaning,
                NextCodeOnEnd = (endMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode)
                    ? (string.IsNullOrWhiteSpace(nextCodeOnEnd) ? "30" : nextCodeOnEnd.Trim())
                    : null,
                IsPlanningCutoff = isPlanningCutoff,
                PlanningCutoffShiftDays = isPlanningCutoff ? Math.Max(1, cutoffShiftDays) : 0
            };

        static void Add(AppDbContext db, TimesheetLane lane, Guid from, Guid to, string by, DateTime at)
            => db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                Lane = lane,
                FromCodeId = from,
                ToCodeId = to,
                CreatedBy = by,
                CreatedAtUtc = at
            });

        static void AddAll(AppDbContext db, TimesheetLane lane, Guid from, IEnumerable<Guid> toIds, string by, DateTime at)
        {
            foreach (var to in toIds.Distinct())
                Add(db, lane, from, to, by, at);
        }
    }
}
