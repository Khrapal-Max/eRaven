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

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.TimesheetViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace eRaven.Components.Pages.Timesheet;

public partial class TimesheetPage : ComponentBase, IDisposable
{
    // ============================= 1) DI, стан =============================
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private ILogger<TimesheetPage> Logger { get; set; } = default!;

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
            var toUtc = ToUtcMidnight(BuiltEndLocal); // exclusive

            foreach (var p in persons)
            {
                var hist = await PersonStatusService.GetHistoryAsync(p.Id, _cts.Token) ?? [];
                var days = BuildDailyCellsWithBaseline(hist, fromUtc, toUtc);

                if (days.Length == 0 || days.All(d => d is null || d.Code is null))
                    continue;

                FillLeadingGapsWithNb(days);
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

                // ЕКСПОРТ: лише коди, рівно BuiltDaysInMonth
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
            if (!TryHandleKnownException(ex, "Не вдалося побудувати табель"))
            {
                throw;
            }
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
    private DayCell[] BuildDailyCellsWithBaseline(
        IReadOnlyList<PersonStatus> history,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var daysCount = (toUtc - fromUtc).Days;
        var result = new DayCell[daysCount];
        if (daysCount <= 0) return result;
        if (history is null || history.Count == 0) return result;

        var ordered = history
            .OrderBy(s => s.OpenDate)
            .ThenBy(s => s.Sequence)
            .ToList();

        var baseline = ordered.LastOrDefault(s => s.OpenDate <= fromUtc);
        string? currentCode = baseline?.StatusKind?.Code?.Trim() ?? CodeForKind(baseline?.StatusKindId ?? 0);
        string? currentTitle = baseline?.StatusKind?.Name ?? NameForKind(baseline?.StatusKindId ?? 0);
        string? currentNote = baseline?.Note;

        var inRange = ordered.Where(s => s.OpenDate >= fromUtc && s.OpenDate < toUtc).ToList();
        var idx = 0;

        for (int i = 0; i < daysCount; i++)
        {
            var dayUtc = fromUtc.AddDays(i);

            while (idx < inRange.Count && inRange[idx].OpenDate <= dayUtc)
            {
                var s = inRange[idx++];
                currentCode = s.StatusKind?.Code?.Trim() ?? CodeForKind(s.StatusKindId);
                currentTitle = s.StatusKind?.Name ?? NameForKind(s.StatusKindId);
                currentNote = s.Note;
            }

            result[i] = new DayCell
            {
                Code = string.IsNullOrWhiteSpace(currentCode) ? null : currentCode,
                Title = currentTitle,
                Note = string.IsNullOrWhiteSpace(currentNote) ? null : currentNote?.Trim()
            };
        }

        return result;
    }

    private void FillLeadingGapsWithNb(DayCell[] days)
    {
        var first = Array.FindIndex(days, d => d is { Code: not null });
        if (first <= 0) return;

        var title = NameForCode("нб") ?? "нб";
        for (int i = 0; i < first; i++)
        {
            days[i] ??= new DayCell();
            days[i]!.Code = "нб";
            days[i]!.Title = title;
        }
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

    private bool TryHandleKnownException(Exception ex, string message)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return false;
            case System.ComponentModel.DataAnnotations.ValidationException:
            case FluentValidation.ValidationException:
            case InvalidOperationException:
            case ArgumentException:
            case HttpRequestException:
                Toast.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
