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
                // FACT (колишній MAIN)
                // ----------------------------

                // базовий факт-стан
                Code("Т",  "Тил",   10, TimesheetEndDateMeaning.LastDayOfThisCode),
                Code("30", "В районі", 10, TimesheetEndDateMeaning.LastDayOfThisCode),

                // “день повернення це ще подія”
                Code("ВДР",  "Відрядження",        20, TimesheetEndDateMeaning.LastDayOfThisCode),
                Code("ВЛК",  "Проходження ВЛК",    40, TimesheetEndDateMeaning.LastDayOfThisCode),
                Code("МСЕК", "Проходження МСЕК",   41, TimesheetEndDateMeaning.LastDayOfThisCode),
                Code("ЛХ",   "Лікування по хворобі",50, TimesheetEndDateMeaning.LastDayOfThisCode),
                Code("ЛП",   "Лікування по пораненню",51, TimesheetEndDateMeaning.LastDayOfThisCode),

                // “день повернення це наступна подія” (30 з цієї дати)
                Code("ВП",  "Відпустка",                 30, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),
                Code("ВПХ", "Відпустка по хворобі",      31, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),
                Code("ВПП", "Відпустка по пораненню",    32, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30"),

                // інцидентні (вихід потім у 30/РОЗПОР, але НЕ в НБ)
                Code("БВ",        "Безвісти",                60, TimesheetEndDateMeaning.FirstDayOfNextCode),
                Code("П",         "Полон",                   61, TimesheetEndDateMeaning.FirstDayOfNextCode),
                Code("А",         "Арешт",                   62, TimesheetEndDateMeaning.FirstDayOfNextCode),
                Code("БВ (СЗЧ)",  "Безпідставно відсутній",  63, TimesheetEndDateMeaning.FirstDayOfNextCode),
                Code("СЗЧ",       "СЗЧ",                     64, TimesheetEndDateMeaning.FirstDayOfNextCode),

                // адмін/фінальні
                Code("РОЗПОР", "Розпорядження", 80, TimesheetEndDateMeaning.FirstDayOfNextCode, nextCodeOnEnd: "30", isTerminal: true),
                Code("200",    "Загибель",      99, TimesheetEndDateMeaning.LastDayOfThisCode, isTerminal: true),

                // ----------------------------
                // Завдання/факти, що впливають на план (поки лишаємо як факт-коди)
                // ----------------------------
                Code("100",  "Затверджене завдання (факт)", 110, TimesheetEndDateMeaning.LastDayOfThisCode),

                Code("Ф100", "Ф100 (факт поранення)", 120, TimesheetEndDateMeaning.LastDayOfThisCode,
                    isPlanningCutoff: true, cutoffShiftDays: 1),

                Code("ПБД",  "ПБД (факт)", 130, TimesheetEndDateMeaning.LastDayOfThisCode,
                    requiresReference: true,
                    isPlanningCutoff: true, cutoffShiftDays: 1),
            };

            foreach (var c in codes)
            {
                c.Id = Guid.NewGuid();
                c.CreatedBy = author;
                c.CreatedAtUtc = now;

                // на всяк випадок нормалізуємо
                c.Code = (c.Code ?? string.Empty).Trim();
                c.Title = (c.Title ?? string.Empty).Trim();
            }

            db.TimesheetCodes.AddRange(codes);
            await db.SaveChangesAsync(ct);
        }

        // 2) Transitions (без НБ, без -> НБ)
        if (!await db.TimesheetCodeTransitions.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            const string author = "seed";

            // тільки активні коди
            var codes = await db.TimesheetCodes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(ct);

            var byCode = codes.ToDictionary(x => x.Code, x => x.Id, StringComparer.Ordinal);

            void AddFrom(string fromCode, params string[] toCodes)
                => AddAll(db, byCode[fromCode], toCodes.Select(c => byCode[c]), author, now);

            // 30 -> все (крім себе)
            var id30 = byCode["30"];
            AddAll(db, id30, codes.Select(x => x.Id).Where(x => x != id30), author, now);

            // Тил дозволені: ВДР, 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ)
            AddFrom("Т", "ВДР", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)");

            // ВДР дозволені: 30, Т, ЛХ, СЗЧ, 200, А, БВ (СЗЧ)
            AddFrom("ВДР", "Т", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)");

            // ВП: Т, 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ)
            AddFrom("ВП", "Т", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)");

            // ВПХ: Т, 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ), ВЛК
            AddFrom("ВПХ", "Т", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК");

            // ВПП: Т, 30, ЛП, СЗЧ, 200, А, БВ (СЗЧ), ВЛК
            AddFrom("ВПП", "Т", "30", "ЛП", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК");

            // ВЛК: Т, 30, ЛХ, СЗЧ, 200, А, БВ (СЗЧ), ЛП, ВПХ, ВПП
            AddFrom("ВЛК", "Т", "30", "ЛХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ЛП", "ВПХ", "ВПП");

            // МСЕК: Т, 30
            AddFrom("МСЕК", "Т", "30");

            // ЛХ: Т, 30, ВПХ, СЗЧ, 200, А, БВ (СЗЧ), ВЛК, РОЗПОР
            AddFrom("ЛХ", "Т", "30", "ВПХ", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК", "РОЗПОР");

            // ЛП: Т, 30, ВПП, СЗЧ, 200, А, БВ (СЗЧ), ВЛК, РОЗПОР
            AddFrom("ЛП", "Т", "30", "ВПП", "СЗЧ", "200", "А", "БВ (СЗЧ)", "ВЛК", "РОЗПОР");

            // БВ: Т, 30, П, СЗЧ, 200, БВ (СЗЧ), РОЗПОР
            AddFrom("БВ", "Т", "30", "П", "СЗЧ", "200", "БВ (СЗЧ)", "РОЗПОР");

            // П: Т, 30, РОЗПОР
            AddFrom("П", "Т", "30", "РОЗПОР");

            // А: Т, 30, РОЗПОР
            AddFrom("А", "Т", "30", "РОЗПОР");

            // БВ (СЗЧ): Т, 30, РОЗПОР, СЗЧ, 200
            AddFrom("БВ (СЗЧ)", "Т", "30", "РОЗПОР", "СЗЧ", "200");

            // СЗЧ: Т, 30, РОЗПОР
            AddFrom("СЗЧ", "Т", "30", "РОЗПОР");

            // РОЗПОР: Т, 30
            AddFrom("РОЗПОР", "Т", "30");

            // 200: переходів немає (термінальний факт)

            // 100: Т, 30, БВ, ПБД, 200, БВ (СЗЧ), П, Ф100
            AddFrom("100", "Т", "30", "БВ", "ПБД", "200", "БВ (СЗЧ)", "П", "Ф100");

            // Ф100: Т, 30, ЛП, ЛХ
            AddFrom("Ф100", "Т", "30", "ЛП", "ЛХ");

            // ПБД: Т, 30, 100
            AddFrom("ПБД", "Т", "30", "100");

            await db.SaveChangesAsync(ct);
        }

        static TimesheetCodeDefinition Code(
            string code,
            string title,
            int sort,
            TimesheetEndDateMeaning endMeaning,
            string? nextCodeOnEnd = null,
            bool isTerminal = false,
            bool requiresReference = false,
            bool requiresNote = false,
            bool isPlanningCutoff = false,
            int cutoffShiftDays = 1)
            => new()
            {
                Code = code,
                Title = title,
                SortOrder = sort,
                IsTerminal = isTerminal,
                RequiresReference = requiresReference,
                RequiresNote = requiresNote,

                // End behavior
                EndDateMeaning = endMeaning,
                NextCodeOnEnd = endMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode
                    ? (string.IsNullOrWhiteSpace(nextCodeOnEnd) ? "30" : nextCodeOnEnd.Trim())
                    : null,

                // Planning cutoff
                IsPlanningCutoff = isPlanningCutoff,
                PlanningCutoffShiftDays = isPlanningCutoff ? Math.Max(1, cutoffShiftDays) : 0,

                // defaults
                IsActive = true
            };

        static void Add(AppDbContext db, Guid from, Guid to, string by, DateTime at)
            => db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                FromCodeId = from,
                ToCodeId = to,
                CreatedBy = by,
                CreatedAtUtc = at
            });

        static void AddAll(AppDbContext db, Guid from, IEnumerable<Guid> toIds, string by, DateTime at)
        {
            foreach (var to in toIds.Where(x => x != Guid.Empty).Distinct())
                Add(db, from, to, by, at);
        }
    }
}
