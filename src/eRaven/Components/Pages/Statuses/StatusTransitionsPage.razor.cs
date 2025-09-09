//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// StatusTransitionsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Application.ViewModels;
using eRaven.Application.ViewModels.PersonStatusViewModel;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace eRaven.Components.Pages.Statuses;

public partial class StatusTransitionsPage : ComponentBase, IDisposable
{
    // =============================
    // Дані
    // =============================
    private List<Person> _all = [];
    private List<Person> _filtered = [];
    private IReadOnlyList<StatusKind> _statuses = [];

    protected ObservableCollection<Person> Items { get; } = [];

    // =============================
    // Модалка
    // =============================
    private bool _isStatusModalOpen;
    private Person? _modalPerson;
    private PersonStatus? _modalCurrentStatus;
    private readonly List<StatusKind> _mapStatuses = [];

    // =============================
    // UI state / DI / infra
    // =============================

    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] private IStatusTransitionService StatusTransitionService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // =============================
    // Життєвий цикл
    // =============================
    protected override async Task OnInitializedAsync()
    {
        await ReloadAllAsync();

        _statuses = await StatusKindService.GetAllAsync();

        ApplyLocalFilter();
    }

    // =============================
    // Модалка
    // =============================
    protected async Task OpenSetStatus(Person p)
    {
        try
        {
            SetBusy(true);

            _modalPerson = p;

            // Поточний інтервал статусу (може бути відкритий)
            _modalCurrentStatus = await PersonStatusService.GetActiveAsync(p.Id, _cts.Token);

            // Дозволені статуси
            _mapStatuses.Clear();
            _mapStatuses.AddRange(await BuildAllowedStatusesAsync(_modalCurrentStatus, _cts.Token));

            _isStatusModalOpen = true;

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося підготувати форму зміни статусу: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HandleStatusSubmitAsync(SetPersonStatusViewModel vm)
    {
        try
        {
            SetBusy(true);

            // 1) Нормалізуємо обрану дату до 00:00 локального дня -> UTC
            var openUtc = ToUtcFromLocalMidnight(vm.Moment);

            // 2) Мапимо у доменну модель PersonStatus
            var ps = new PersonStatus
            {
                Id = Guid.Empty,
                PersonId = vm.PersonId,
                StatusKindId = vm.StatusId,
                OpenDate = openUtc,
                CloseDate = null,
                Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note!.Trim(),
                IsActive = true,
                Author = string.IsNullOrWhiteSpace(vm.Author) ? null : vm.Author!.Trim(),
                Modified = DateTime.UtcNow
            };

            // 3) Виклик сервісу
            _ = await PersonStatusService.SetStatusAsync(ps, _cts.Token);

            // 4) UX: закрити модаль, рефреш
            _isStatusModalOpen = false;
            _modalPerson = null;
            _modalCurrentStatus = null;
            _mapStatuses.Clear();

            await ReloadAllAsync();
            ApplyLocalFilter();

            Toast.ShowSuccess("Статус збережено.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося зберегти статус: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private Task HandleStatusCloseAsync()
    {
        _isStatusModalOpen = false;
        _modalPerson = null;
        _modalCurrentStatus = null;
        _mapStatuses.Clear();
        StateHasChanged();
        return Task.CompletedTask;
    }

    // =============================
    // Імпорт списку 
    // =============================
    protected async Task<ImportReportViewModel> ProcessImportedStatusesAsync(IReadOnlyList<PersonStatusImportView> rows)
    {
        int ok = 0, fail = 0;
        var errors = new List<string>();

        foreach (var (row, idx) in rows.Select((r, i) => (r, i: i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Rnokpp))
                    throw new ArgumentException("RNOKPP порожній.");
                if ((row.StatusKindId ?? 0) == 0 && string.IsNullOrWhiteSpace(row.StatusCode))
                    throw new ArgumentException("Не вказано StatusKindId або StatusCode.");
                if (row.FromDateLocal == default)
                    throw new ArgumentException("Не вказано дату.");

                // TODO: інтеграція з пошуком Person за RNOKPP та SetStatusAsync(...)
                ok++;
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add($"Row {idx}: {ex.Message}");
            }
        }

        await ReloadAllAsync();
        ApplyLocalFilter();

        return new ImportReportViewModel(Added: ok, Updated: 0, Errors: errors);
    }

    protected Task OnImportCompleted(ImportReportViewModel report)
    {
        if ((report.Errors?.Count ?? 0) > 0)
            Toast.ShowError($"Імпорт завершено з помилками: {report.Errors!.Count}. Успішно: {report.Added}");
        else
            Toast.ShowSuccess($"Імпорт успішний. Успішно: {report.Added}");
        return Task.CompletedTask;
    }

    protected Task OnImportBusyChanged(bool busy)
    {
        SetBusy(busy);
        return Task.CompletedTask;
    }

    // =============================
    // Перевантаження списку 
    // =============================
    private async Task ReloadAllAsync()
    {
        try
        {
            SetBusy(true);
            _all = [.. await PersonService.SearchAsync(null, _cts.Token)];
        }
        catch (Exception ex)
        {
            _all.Clear();
            Toast.ShowError($"Не вдалося завантажити картки: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =============================
    // Формування списку дозволених
    // =============================
    private async Task<IReadOnlyList<StatusKind>> BuildAllowedStatusesAsync(PersonStatus? current, CancellationToken ct)
    {
        // 1) Кеш або одноразове завантаження
        var statuses = (_statuses?.Count > 0 ? _statuses : await StatusKindService.GetAllAsync(ct: ct)) ?? [];
        if (statuses.Count == 0) return [];

        // 2) Спец-статуси: «В районі» та «В БР» (спершу за Code, потім за Name)
        static bool eq(string? a, string b) => !string.IsNullOrWhiteSpace(a) && a.Trim().Equals(b, StringComparison.OrdinalIgnoreCase);

        var inDistrict = statuses.FirstOrDefault(s => eq(s.Code, "30") || eq(s.Name, "В районі"));
        var inBr = statuses.FirstOrDefault(s => eq(s.Code, "100") || eq(s.Name, "В БР"));

        // 3) Перший статус: дозволити лише «В районі»
        if (current is null || current.StatusKindId <= 0)
            return inDistrict is null ? Array.Empty<StatusKind>() : [inDistrict];

        // 4) Карта переходів + винести «В БР»
        var toIds = await StatusTransitionService.GetToIdsAsync(current.StatusKindId, ct) ?? [];
        if (inBr is not null) toIds.Remove(inBr.Id);

        // 5) Перетин + сортування (без Distinct — HashSet виключає дублікати)
        return [.. statuses
            .Where(s => toIds.Contains(s.Id))
            .OrderBy(s => s.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
    }

    // =============================
    // Фільтр
    // =============================
    protected Task OnSearchAsync()
    {
        ApplyLocalFilter();
        return Task.CompletedTask;
    }

    private void ApplyLocalFilter()
    {
        IEnumerable<Person> q = _all;

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();

            static bool Has(string? haystack, string needle)
                => !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

            q = q.Where(p =>
                Has(p.FullName, s) ||
                Has(p.Rnokpp, s) ||
                Has(p.Rank, s) ||
                Has(p.Callsign, s) ||
                Has(p.Weapon, s) ||
                Has(p.PositionUnit?.ShortName, s));
        }

        _filtered = [.. q];
        Items.Clear();
        foreach (var p in _filtered) Items.Add(p);
        StateHasChanged();
    }

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    private static DateTime ToUtcFromLocalMidnight(DateTime localDateUnspecified)
    {
        // локальний календарний день -> 00:00 Local -> UTC
        var localMidnight = DateTime.SpecifyKind(localDateUnspecified.Date, DateTimeKind.Local);
        return localMidnight.ToUniversalTime();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
