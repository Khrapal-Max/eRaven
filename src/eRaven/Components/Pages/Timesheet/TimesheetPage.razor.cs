//-----------------------------------------------------------------------------
// Табель (місяць/рік) + експорт кодів (без нотаток у xlsx)
// Групи:
//   1) DI, стан (Working vs Built)
//   2) Життєвий цикл і Rebuild
//   3) Навігація (міняє лише Working-*; перерахунок тільки по кнопці)
//   4) Побудова денних клітинок (baseline + зміни)
//   5) Відображення (кольори/легенда/тултіп)
//   6) Експорт (формуємо TimesheetExportRow з Day01..Day31 тільки кодами)
//   7) Утиліти/прибирання
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Blazored.Toast.Services;
using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.TimesheetViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetPage : ComponentBase, IDisposable
{
    private static readonly IComparer<StatusKind> StatusKindPriorityComparer = Comparer<StatusKind>.Create(StatusPriorityComparer.Compare);
    // ============================= 1) DI, стан =============================
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IPersonStatusReadService PersonStatusReadService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    protected List<TimesheetExportRow> ExportRowsFlex { get; } = [];

    // Working* — вибрані в інпуті значення (без автоперерахунку)
    protected int WorkingYear { get; set; } = DateTime.Today.Year;
    protected int WorkingMonth { get; set; } = DateTime.Today.Month;

    // Built* — зафіксовані місяць/рік, по яких реально побудовано табель
    protected int BuiltYear { get; private set; }
    protected int BuiltMonth { get; private set; }

    protected DateTime BuiltStartLocal => new(BuiltYear, BuiltMonth, 1);
    protected DateTime BuiltEndLocal => BuiltStartLocal.AddMonths(1); // exclusive
    protected int BuiltDaysInMonth => DateTime.DaysInMonth(BuiltYear, BuiltMonth);

    protected bool Busy { get; private set; }

    private IReadOnlyList<StatusKind> _kinds = [];
    protected List<TimesheetRow> Rows { get; } = [];

    // Якщо ВЕСЬ місяць тільки ці коди — людину не показуємо
    private static readonly HashSet<string> ExcludeCodes =
        new(StringComparer.OrdinalIgnoreCase) { "нб", "РОЗПОР" };

    // Коди, що зустрілись (для легенди)
    protected HashSet<string> LegendCodes { get; } = new(StringComparer.OrdinalIgnoreCase);

    // ===================== 2) Життєвий цикл і Rebuild =====================
    protected override async Task OnInitializedAsync()
    {
        // Початково зіб'ємо Built = Working і побудуємо
        BuiltYear = WorkingYear;
        BuiltMonth = WorkingMonth;
        await RebuildAsync();
    }

    /// <summary>Побудова табелю по BuiltYear/BuiltMonth (копіює Working → Built).</summary>
    protected async Task RebuildAsync()
    {
        try
        {
            SetBusy(true);

            BuiltYear = WorkingYear;
            BuiltMonth = WorkingMonth;

            Rows.Clear();
            ExportRowsFlex.Clear();
            LegendCodes.Clear();

            _kinds = await StatusKindService.GetAllAsync(ct: _cts.Token) ?? [];

            var persons = await PersonService.SearchAsync(null, _cts.Token) ?? [];
            if (persons.Count == 0) return;

            var fromUtc = ToUtcMidnight(BuiltStartLocal);

            var notPresentKind = await PersonStatusReadService.ResolveNotPresentAsync(_cts.Token);
            var monthMap = await PersonStatusReadService.ResolveMonthAsync(
                persons.Select(p => p.Id),
                BuiltYear,
                BuiltMonth,
                _cts.Token);

            foreach (var p in persons)
            {
                if (!monthMap.TryGetValue(p.Id, out var monthStatus))
                    continue;

                var days = BuildDailyCells(monthStatus.Days, fromUtc, notPresentKind, monthStatus.FirstPresenceUtc);
                if (days.Length == 0 || days.All(d => d is null || d.Code is null))
                    continue;
                    
                if (IsEntireMonthExcluded(days))
                    continue;

                Rows.Add(new TimesheetRow
                {
                    PersonId = p.Id,
                    FullName = p.FullName,
                    Rank = p.Rank,
                    Rnokpp = p.Rnokpp,
                    Days = days
                });

                var codes = new string[BuiltDaysInMonth];
                for (int i = 0; i < BuiltDaysInMonth; i++)
                    codes[i] = days[i]?.Code ?? string.Empty;

                ExportRowsFlex.Add(new TimesheetExportRow
                {
                    FullName = p.FullName,
                    Rank = p.Rank,
                    Rnokpp = p.Rnokpp,
                    Days = codes
                });
            }
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося побудувати табель: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ====================== 3) Навігація (тільки Working) ======================
    protected void PrevMonth()
    {
        var d = new DateTime(WorkingYear, WorkingMonth, 1).AddMonths(-1);
        WorkingYear = d.Year;
        WorkingMonth = d.Month;
        // НЕ перераховуємо — лише за кнопкою «Побудувати»
    }

    protected void NextMonth()
    {
        var d = new DateTime(WorkingYear, WorkingMonth, 1).AddMonths(1);
        WorkingYear = d.Year;
        WorkingMonth = d.Month;
        // НЕ перераховуємо — лише за кнопкою «Побудувати»
    }

    // ========== 4) Побудова денних клітинок (baseline + зміни) ==========
    private DayCell[] BuildDailyCells(
        PersonStatus?[] monthStatuses,
        DateTime fromUtc,
        StatusKind? notPresentKind,
        DateTime? firstPresenceUtc)
    {
        if (monthStatuses is null || monthStatuses.Length == 0)
            return Array.Empty<DayCell>();
            
        var result = new DayCell[monthStatuses.Length];

        var notPresentCode = notPresentKind?.Code?.Trim();
        var notPresentTitle = string.IsNullOrWhiteSpace(notPresentKind?.Name)
            ? (string.IsNullOrWhiteSpace(notPresentCode) ? null : NameForCode(notPresentCode) ?? notPresentCode)
            : notPresentKind!.Name;

        for (int i = 0; i < monthStatuses.Length; i++)
        {
            var endOfDayUtc = fromUtc.AddDays(i + 1).AddTicks(-1);
            var status = monthStatuses[i];

            if (firstPresenceUtc is null || endOfDayUtc < firstPresenceUtc.Value || status is null)
            {
                if (string.IsNullOrWhiteSpace(notPresentCode))
                {
                    result[i] = new DayCell();
                    continue;
                }

                result[i] = new DayCell
                {
                    Code = notPresentCode,
                    Title = notPresentTitle ?? notPresentCode,
                    Note = null
                };
                continue;
            }

            var code = status.StatusKind?.Code?.Trim() ?? CodeForKind(status.StatusKindId);
            var title = status.StatusKind?.Name ?? NameForKind(status.StatusKindId);
            var note = string.IsNullOrWhiteSpace(status.Note) ? null : status.Note!.Trim();

            result[i] = new DayCell
            {
                Code = string.IsNullOrWhiteSpace(code) ? null : code,
                Title = title,
                Note = note
            };
        }

        return result;
    }

    // ================== 5) Відображення (кольори/легенда/тултіп) ==================
    private void TouchLegend(string? code)
    {
        if (!string.IsNullOrWhiteSpace(code) && !code.Equals("30", StringComparison.OrdinalIgnoreCase))
            LegendCodes.Add(code.Trim());
    }

    private static readonly Dictionary<string, string> BadgeByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["100"] = "badge rounded-pill text-bg-primary",   // синій
        ["нб"] = "badge rounded-pill text-bg-info",      // блакитний
        ["РОЗПОР"] = "badge rounded-pill text-bg-info",     // блакитний
        ["ВДР"] = "badge rounded-pill text-bg-secondary", // сірий
        ["В"] = "badge rounded-pill text-bg-success",   // зелений
        ["Л_Х"] = "badge rounded-pill text-bg-warning",   // жовтий
        ["Л_Б"] = "badge rounded-pill text-bg-warning",   // жовтий
        ["БВ"] = "badge rounded-pill text-bg-danger",    // червоний
        ["П"] = "badge rounded-pill text-bg-danger",
        ["200"] = "badge rounded-pill text-bg-danger",
        ["А"] = "badge rounded-pill text-bg-danger",
        ["СЗЧ"] = "badge rounded-pill text-bg-danger"
    };

    protected string GetBadgeClass(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("30", StringComparison.OrdinalIgnoreCase))
            return "d-inline-block px-1 small"; // «30» — без підсвітки

        if (BadgeByCode.TryGetValue(code.Trim(), out var cls))
            return $"{cls} px-2 py-1";

        return "badge rounded-pill text-bg-primary px-2 py-1";
    }

    protected string GetStatusTitle(string code)
    {
        var name = _kinds.FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;
        return string.IsNullOrWhiteSpace(name) ? code : $"{code} — {name}";
    }

    protected static string BuildTitle(DayCell? cell)
    {
        if (cell is null || string.IsNullOrWhiteSpace(cell.Code)) return string.Empty;
        var head = string.IsNullOrWhiteSpace(cell.Title) ? cell.Code! : $"{cell.Code} — {cell.Title}";
        return string.IsNullOrWhiteSpace(cell.Note) ? head : $"{head}: {cell.Note}";
    }

    // ================== 6) Експорт (Day01..Day31 тільки коди) ==================
    // TimesheetExportRow — у твоєму окремому файлі ViewModels (використовуємо тут)

    // ===================== 7) Утиліти/прибирання =====================
    private static DateTime ToUtcMidnight(DateTime localDate)
        => new(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Utc);

    private string? CodeForKind(int statusKindId)
        => _kinds.FirstOrDefault(k => k.Id == statusKindId)?.Code?.Trim();

    private string? NameForKind(int statusKindId)
        => _kinds.FirstOrDefault(k => k.Id == statusKindId)?.Name;

    private string? NameForCode(string code)
        => _kinds.FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;

    private static bool IsEntireMonthExcluded(DayCell[] days)
        => days.Length > 0 && days.All(c => c?.Code is not null && ExcludeCodes.Contains(c!.Code!));

    private void OnExportBusyChanged(bool exporting)
        => SetBusy(exporting); // 👈 фікс: не тримаємо Busy після експорту

    private void SetBusy(bool v)
    {
        Busy = v;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
