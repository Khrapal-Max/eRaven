//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicySeed (latest policy model: RoleCode + TimesheetUiStyle; NB is derived)
//-----------------------------------------------------------------------------

using eRaven.Domain.Consts;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure;

/// <summary>
/// Seeds timesheet policy:
/// - Codes (timesheet_codes)
/// - Transition matrix (timesheet_code_transitions)
///
/// IMPORTANT:
/// - "НБ" is a derived state (TimesheetDerivedCodes.NotInTimesheet) and MUST NOT be seeded as a code.
/// - SystemCode (e.g. "100") is seeded as a code but is not intended for manual transitions.
/// - Emergency codes are seeded as EmergencyCode and shown globally (not tied to current state).
/// - Transition matrix is seeded from the authoritative "статуси переходи 3.xlsx" mapping.
///   When seeding transitions we persist:
///   - TransitionCode -> TransitionCode edges
///   - EmergencyCode -> TransitionCode edges (to support leaving аварійні стани)
///   We intentionally do NOT persist edges *to* EmergencyCode because emergency codes are global options.
/// </summary>
public static class TimesheetPolicySeed
{
    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await EnsureCodesAsync(db, ct);
        await EnsureTransitionsAsync(db, ct);
    }

    //======================================================================
    // Codes
    //======================================================================

    private static async Task EnsureCodesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.TimesheetCodes.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        const string author = "system";

        var codes = new List<TimesheetCodeDefinition>
        {
            // -------------------------------------------------------------
            // Transition (work) codes
            // -------------------------------------------------------------
            New("Т", "Базовий стан (служба/перебування у тилу).", 10, 10,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("30", "Базовий “польовий” стан (“день у районі виконання завдань РС ОС”).", 20, 20,
                RoleCode.TransitionCode, TimesheetUiStyle.Ready),

            New("ВДР", "Фіксує період відрядження.", 30, 30,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ВЛК", "Фіксує період проходження ВЛК.", 40, 40,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("МСЕК", "Фіксує період проходження МСЕК.", 41, 41,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ЛХ", "Період лікування/перебування на лікуванні (хвороба).", 50, 50,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ЛП", "Період лікування/перебування на лікуванні (поранення).", 51, 51,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ВП", "Період відпустки (щорічна або сімейна).", 60, 60,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ВПХ", "Період відпустки (хвороба).", 61, 61,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("ВПП", "Період відпустки (поранення).", 62, 62,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            New("РОЗПОР", "Адміністративний стан “розпорядження”.", 80, 80,
                RoleCode.TransitionCode, TimesheetUiStyle.Warning),

            // -------------------------------------------------------------
            // System code(s) (not manual)
            // -------------------------------------------------------------
            New("100", "Системний стан завдання (ставиться документом; ручні події блокуються).", 90, 0,
                RoleCode.SystemCode, TimesheetUiStyle.Danger),          

            // -------------------------------------------------------------
            // Emergency codes (global options)
            // -------------------------------------------------------------
            New("Ф100", "Факт/позначка поранення, травмування, прояву хвороби (службовий факт).", 70, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("БВ", "Безвісти (аварійний код).", 71, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("П", "Полон (аварійний код).", 72, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("А", "Арешт (аварійний код).", 73, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("БВ (СЗЧ)", "Безвісти (СЗЧ) (аварійний код).", 74, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("СЗЧ", "СЗЧ (аварійний код).", 75, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger),

            New("200", "Загиблий (термінальний аварійний код).", 99, 0,
                RoleCode.EmergencyCode, TimesheetUiStyle.Danger, isTerminal: true),
        };

        foreach (var c in codes)
        {
            c.Id = Guid.NewGuid();
            c.CreatedBy = author;
            c.CreatedAtUtc = now;

            c.Code = c.Code.Trim();
            c.Title = c.Title.Trim();
            c.Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim();
        }

        db.TimesheetCodes.AddRange(codes);
        await db.SaveChangesAsync(ct);
    }

    private static TimesheetCodeDefinition New(
        string code,
        string title,
        int sort,
        int priority,
        RoleCode roleCode,
        TimesheetUiStyle uiStyle,
        bool isTerminal = false)
        => new()
        {
            Code = code,
            Title = title,
            SortOrder = sort,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = true,
            RoleCode = roleCode,
            UiStyle = uiStyle
        };

    //======================================================================
    // Transitions (matrix)
    //======================================================================

    private static async Task EnsureTransitionsAsync(AppDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        const string author = "system";

        var defs = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Code, x => x, ct);

        // Add missing transitions if DB already has some (seed is additive / idempotent).
        var existing = await db.TimesheetCodeTransitions
            .AsNoTracking()
            .Select(x => new { x.FromCodeId, x.ToCodeId, x.StartShiftDays })
            .ToListAsync(ct);

        var existingSet = new HashSet<(Guid From, Guid To, int Shift)>(
            existing.Select(x => (x.FromCodeId, x.ToCodeId, x.StartShiftDays)));

        var list = new List<TimesheetCodeTransition>();

        void Add(string from, string to, int shift)
        {
            if (!defs.TryGetValue(from, out var fromDef))
                return;

            if (!defs.TryGetValue(to, out var toDef))
                return;

            // shift 0 = "З дати події"
            // shift 1 = "Ще поточний"
            if (shift is not (0 or 1))
                return;

            // Never allow "НБ" in matrix, even if it exists in DB by mistake
            if (string.Equals(fromDef.Code, TimesheetDerivedCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(toDef.Code, TimesheetDerivedCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
                return;

            // Emergency codes are global options. We don't persist edges *to* EmergencyCode,
            // because the UI and policy treat them as always allowed.
            if (toDef.RoleCode == RoleCode.EmergencyCode)
                return;

            // Persist only:
            // - TransitionCode -> TransitionCode
            // - EmergencyCode  -> TransitionCode
            var allowed =
                (fromDef.RoleCode == RoleCode.TransitionCode && toDef.RoleCode == RoleCode.TransitionCode)
                || (fromDef.RoleCode == RoleCode.EmergencyCode && toDef.RoleCode == RoleCode.TransitionCode);

            if (!allowed)
                return;

            var key = (fromDef.Id, toDef.Id, shift);
            if (existingSet.Contains(key))
                return;

            existingSet.Add(key);

            list.Add(new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                FromCodeId = fromDef.Id,
                ToCodeId = toDef.Id,
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
        // Full mapping
        // shift 0 = "З дати події"
        // shift 1 = "Ще поточний"
        // =================================================================

        AddFrom("Т",
            shift0: ["30", "ВДР", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПХ", "ВПП",
                     "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "Ф100"]);

        AddFrom("30",
            shift0: ["Т", "ВДР", "ВЛК", "МСЕК", "ЛХ", "ЛП", "ВП", "ВПХ", "ВПП",
                     "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200", "100", "Ф100"]);

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

        AddFrom("Ф100",
            shift0: ["Т", "30", "ЛХ", "ЛП", "БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "РОЗПОР", "200"]);

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

        // 200 terminal — no transitions in the source mapping

        AddFrom("100",
            shift0: ["БВ", "П", "А", "БВ (СЗЧ)", "СЗЧ", "200", "Ф100"],
            shift1: ["30"]);

        if (list.Count == 0)
            return;

        db.TimesheetCodeTransitions.AddRange(list);
        await db.SaveChangesAsync(ct);
    }

}
