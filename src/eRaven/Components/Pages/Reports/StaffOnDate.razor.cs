// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// Reports → StaffOnDatePage (code-behind)
// Логіка:
//  • обираємо дату → “Побудувати” → збираємо усіх та їхній статус на дату
//  • виключаємо з таблиці коди "нб" і "РОЗПОР"
//  • сортування: спочатку за індексом посади (PositionUnit.Code), потім за повною назвою
//  • експорт: плоска модель без стилів/кольорів (ті самі колонки)
// -----------------------------------------------------------------------------


using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StaffOnDateViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace eRaven.Components.Pages.Reports;

public partial class StaffOnDate : ComponentBase, IDisposable
{
    // ============================ DI ============================
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private ILogger<StaffOnDate> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    // =========================== State ==========================
    protected bool Busy { get; private set; }

    /// <summary>Дата у локалі; інтерпретуємо як 00:00 UTC цього календарного дня.</summary>
    protected DateTime DateLocal { get; set; } = DateTime.Today;

    private IReadOnlyList<StatusKind> _kinds = [];
    protected List<ReportRow> Rows { get; } = [];

    /// <summary>Коди, які повністю приховуємо зі звіту.</summary>
    private static readonly HashSet<string> ExcludeCodes =
        new(StringComparer.OrdinalIgnoreCase) { "нб", "РОЗПОР" };

    // ========================= Lifecycle ========================
    protected override Task OnInitializedAsync()
    {
        // Будуємо тільки по кліку “Побудувати”.
        return Task.CompletedTask;
    }

    protected async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            Rows.Clear();

            // 1) Довідник статусів
            _kinds = await StatusKindService.GetAllAsync(ct: _cts.Token) ?? [];

            // 2) Перелік осіб
            var persons = await PersonService.SearchAsync(null, _cts.Token) ?? [];
            if (persons.Count == 0) return;

            // ✅ лишаємо тільки тих, хто на посаді (поточна посада існує і активна)
            persons = [.. persons.Where(p => p.PositionUnit is not null && p.PositionUnit.IsActived)];

            var atUtc = ToUtcMidnight(DateLocal);

            // 4) Формування рядків
            foreach (var p in persons)
            {
                var hist = await PersonStatusService.GetHistoryAsync(p.Id, _cts.Token) ?? [];
                var status = GetStatusOnDate(hist, atUtc);

                // Пропускаємо виключені коди
                var code = status?.Code?.Trim();
                if (!string.IsNullOrWhiteSpace(code) && ExcludeCodes.Contains(code!))
                    continue;

                var row = new ReportRow
                {
                    // Посада
                    PositionCode = p.PositionUnit?.Code,
                    PositionShort = p.PositionUnit?.ShortName,
                    PositionFull = p.PositionUnit?.FullName,
                    SpecialNumber = p.PositionUnit?.SpecialNumber,

                    // Людина
                    FullName = p.FullName,
                    Rank = p.Rank,
                    Rnokpp = p.Rnokpp,
                    Callsign = p.Callsign,
                    BZVP = p.BZVP,
                    Weapon = p.Weapon,

                    // Статус на дату
                    StatusCode = status?.Code,
                    StatusName = status?.Name,
                    StatusNote = status?.Note
                };

                Rows.Add(row);
            }

            // 5) Сортування (індекс посади → повна назва)
            Rows.Sort(static (a, b) =>
            {
                var c = string.Compare(a.PositionCode, b.PositionCode, StringComparison.OrdinalIgnoreCase);
                return c != 0 ? c : string.Compare(a.PositionFull, b.PositionFull, StringComparison.OrdinalIgnoreCase);
            });
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося побудувати звіт"))
            {
                throw;
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ==================== Статус на конкретну дату ====================
    private StatusOnDateViewModel? GetStatusOnDate(IReadOnlyList<PersonStatus> history, DateTime atUtc)
    {
        if (history is null || history.Count == 0) return null;

        // Останній валідний запис із OpenDate <= atUtc (за OpenDate DESC, Sequence DESC)
        var s = history
            .OrderByDescending(x => x.OpenDate)
            .ThenByDescending(x => x.Sequence)
            .FirstOrDefault(x => x.OpenDate <= atUtc);

        if (s is null) return null;

        // Основні поля з навігації; fallback — з довідника
        var code = s.StatusKind?.Code?.Trim();
        var name = s.StatusKind?.Name;

        if (string.IsNullOrWhiteSpace(code))
        {
            var sk = _kinds.FirstOrDefault(k => k.Id == s.StatusKindId);
            code = sk?.Code?.Trim();
            name ??= sk?.Name;
        }

        return new StatusOnDateViewModel
        {
            Code = code,
            Name = name,
            Note = string.IsNullOrWhiteSpace(s.Note) ? null : s.Note!.Trim()
        };
    }

    // ===================== Відображення (кольори) =====================
    private static readonly Dictionary<string, string> BadgeByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["100"] = "badge rounded-pill text-bg-primary",   // синій
        ["нб"] = "badge rounded-pill text-bg-info",       // блакитний
        ["РОЗПОР"] = "badge rounded-pill text-bg-info",   // блакитний
        ["ВДР"] = "badge rounded-pill text-bg-secondary", // сірий
        ["В"] = "badge rounded-pill text-bg-success",     // зелений
        ["Л_Х"] = "badge rounded-pill text-bg-warning",   // жовтий
        ["Л_Б"] = "badge rounded-pill text-bg-warning",   // жовтий
        ["БВ"] = "badge rounded-pill text-bg-danger",     // червоний
        ["П"] = "badge rounded-pill text-bg-danger",
        ["200"] = "badge rounded-pill text-bg-danger",
        ["А"] = "badge rounded-pill text-bg-danger",
        ["СЗЧ"] = "badge rounded-pill text-bg-danger"
    };

    protected string GetBadgeClass(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("30", StringComparison.OrdinalIgnoreCase))
            return "d-inline-block px-1 small"; // «30» — без підсвітки

        return BadgeByCode.TryGetValue(code.Trim(), out var cls)
            ? $"{cls} px-2 py-1"
            : "badge rounded-pill text-bg-primary px-2 py-1";
    }

    protected string GetStatusTitle(string code)
    {
        var name = _kinds.FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;
        return string.IsNullOrWhiteSpace(name) ? code : $"{code} — {name}";
    }

    // ========================== Утиліти ==========================
    private static DateTime ToUtcMidnight(DateTime localDate)
    {
        var d = localDate.Date;
        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private void OnExportBusyChanged(bool exporting) => SetBusy(exporting || Busy);

    private void SetBusy(bool v)
    {
        Busy = v;
        StateHasChanged();
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
