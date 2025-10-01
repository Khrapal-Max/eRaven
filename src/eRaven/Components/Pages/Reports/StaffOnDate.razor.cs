// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// Reports → StaffOnDatePage (code-behind)
// Логіка:
//  • обираємо дату → “Побудувати” → збираємо усіх та їхній статус на дату
//  • виключаємо з таблиці службові коди, що не мають відображатись
//  • сортування: спочатку за індексом посади (PositionUnit.Code), потім за повною назвою
//  • експорт: плоска модель без стилів/кольорів (ті самі колонки)
// -----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StaffOnDateViewModels;
using eRaven.Components.Shared.StatusFormatting;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Reports;

public partial class StaffOnDate : ComponentBase, IDisposable
{
    // ============================ DI ============================
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IPersonStatusReadService PersonStatusReadService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    // =========================== State ==========================
    protected bool Busy { get; private set; }

    /// <summary>Дата у локалі; інтерпретуємо як 00:00 UTC цього календарного дня.</summary>
    protected DateTime DateLocal { get; set; } = DateTime.Today;

    private IReadOnlyList<StatusKind> _kinds = [];
    private Dictionary<int, StatusKind> _kindsById = new();
    protected List<ReportRow> Rows { get; } = [];

    private static readonly HashSet<string> AlwaysExcludedCodes =
        new(StringComparer.OrdinalIgnoreCase) { "РОЗПОР" };

    private string? _notPresentCode;
    private string? _notPresentTitle;

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
            _kindsById = _kinds
                .Where(k => k.Id != default)
                .GroupBy(k => k.Id)
                .Select(g => g.First())
                .ToDictionary(k => k.Id, k => k);

            // 2) Перелік осіб
            var persons = await PersonService.SearchAsync(null, _cts.Token) ?? [];
            if (persons.Count == 0) return;

            // ✅ лишаємо тільки тих, хто на посаді (поточна посада існує і активна)
            persons = [.. persons.Where(p => p.PositionUnit is not null && p.PositionUnit.IsActived)];

            var dayEndUtc = ToUtcEndOfDay(DateLocal);
            var notPresentKind = await PersonStatusReadService.ResolveNotPresentAsync(_cts.Token);
            _notPresentCode = notPresentKind?.Code?.Trim();
            _notPresentTitle = string.IsNullOrWhiteSpace(notPresentKind?.Name)
                ? (_notPresentCode is null ? null : NameForCode(_notPresentCode) ?? _notPresentCode)
                : notPresentKind!.Name;

            // 4) Формування рядків
            var buildTasks = persons
                .Select(p => BuildRowAsync(p, dayEndUtc, notPresentKind, _cts.Token))
                .ToArray();

            var results = await Task.WhenAll(buildTasks);

            foreach (var row in results)
            {
                if (row is not null)
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
            Toast.ShowError($"Не вдалося побудувати звіт: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ==================== Статус на конкретну дату ====================
    private async Task<ReportRow?> BuildRowAsync(Person person, DateTime dayEndUtc, StatusKind? notPresentKind, CancellationToken ct)
    {
        var firstPresenceTask = notPresentKind is null
            ? Task.FromResult<DateTime?>(null)
            : PersonStatusReadService.GetFirstPresenceUtcAsync(person.Id, ct);
        var statusTask = PersonStatusReadService.GetActiveOnDateAsync(person.Id, dayEndUtc, ct);

        await Task.WhenAll(firstPresenceTask, statusTask);

        var firstPresenceUtc = await firstPresenceTask;
        var status = await statusTask;

        StatusKind? statusKind = null;
        string? note = null;

        if (notPresentKind is not null && firstPresenceUtc is not null && dayEndUtc < firstPresenceUtc.Value)
        {
            statusKind = notPresentKind;
        }
        else if (status is not null)
        {
            statusKind = ResolveStatusKind(status);
            note = string.IsNullOrWhiteSpace(status.Note) ? null : status.Note!.Trim();
        }

        if (statusKind is null)
            return null;

        var code = statusKind.Code?.Trim();
        if (statusKind == notPresentKind && string.IsNullOrWhiteSpace(code))
            code = _notPresentCode;
        if (IsExcludedCode(code))
            return null;

        var name = statusKind.Name;
        if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code))
            name = NameForCode(code!);

        if (statusKind == notPresentKind && string.IsNullOrWhiteSpace(name))
            name = _notPresentTitle;

        return new ReportRow
        {
            // Посада
            PositionCode = person.PositionUnit?.Code,
            PositionShort = person.PositionUnit?.ShortName,
            PositionFull = person.PositionUnit?.FullName,
            SpecialNumber = person.PositionUnit?.SpecialNumber,

            // Людина
            FullName = person.FullName,
            Rank = person.Rank,
            Rnokpp = person.Rnokpp,
            Callsign = person.Callsign,
            BZVP = person.BZVP,
            Weapon = person.Weapon,

            // Статус на дату
            StatusCode = code,
            StatusName = name,
            StatusNote = note
        };
    }

    private StatusKind? ResolveStatusKind(PersonStatus status)
    {
        if (status.StatusKind is not null)
            return status.StatusKind;

        if (_kindsById.TryGetValue(status.StatusKindId, out var kind))
            return kind;

        return null;
    }

    // ===================== Відображення (кольори) =====================
    protected string GetBadgeClass(string? code)
        => StatusFormattingHelper.GetBadgeClass(code, _notPresentCode);

    protected string GetStatusTitle(string? code)
        => StatusFormattingHelper.GetStatusTitle(code, _kinds, _notPresentCode, _notPresentTitle);

    private bool IsExcludedCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var trimmed = code.Trim();

        if (AlwaysExcludedCodes.Contains(trimmed))
            return true;

        return _notPresentCode is not null && trimmed.Equals(_notPresentCode, StringComparison.OrdinalIgnoreCase);
    }

    private string? NameForCode(string code)
        => _kinds.FirstOrDefault(k => string.Equals(k.Code, code, StringComparison.OrdinalIgnoreCase))?.Name;

    // ========================== Утиліти ==========================
    private static DateTime ToUtcMidnight(DateTime localDate)
    {
        var d = localDate.Date;
        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime ToUtcEndOfDay(DateTime localDate)
    {
        var startUtc = ToUtcMidnight(localDate);
        return startUtc.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    private void OnExportBusyChanged(bool exporting) => SetBusy(exporting || Busy);

    private void SetBusy(bool v)
    {
        Busy = v;
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
