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
    // =============================  Дані  =============================
    private IReadOnlyList<Person> _all = [];
    private IReadOnlyList<StatusKind> _statuses = [];
    protected ObservableCollection<Person> Filtered { get; } = [];

    // OneShot / Batch
    private bool OneShotMode { get; set; } = true;

    // =============================  Модалка  =============================
    private bool _isStatusModalOpen;
    private Person? _modalPerson;
    private PersonStatus? _modalCurrentStatus;
    private readonly List<StatusKind> _mapStatuses = [];

    // =============================  UI / DI  =============================
    private readonly CancellationTokenSource _cts = new();
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] private IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] private IStatusTransitionService StatusTransitionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // =============================  Життєвий цикл  =============================
    protected override async Task OnInitializedAsync()
    {
        _statuses = await StatusKindService.GetAllAsync();
        await ReloadAllAsync();
        ApplyLocalFilter(); // zero-state (порожньо, доки немає Search)
    }

    // =============================  Модалка  =============================
    protected async Task OpenSetStatus(Person p)
    {
        if (Busy) return;
        try
        {
            SetBusy(true);

            _modalPerson = p;
            _modalCurrentStatus = await PersonStatusService.GetActiveAsync(p.Id, _cts.Token);

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

            var openUtc = ToUtcFromLocalMidnight(vm.Moment);
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

            await PersonStatusService.SetStatusAsync(ps, _cts.Token);

            // Закриваємо модаль, оновлюємо
            _isStatusModalOpen = false;
            _modalPerson = null;
            _modalCurrentStatus = null;
            _mapStatuses.Clear();

            await ReloadAllAsync();

            if (OneShotMode)
            {
                // Одиничний: повернення в zero-state
                Search = string.Empty;
                Filtered.Clear();
            }
            else
            {
                // Пакетний: зберегти фільтр/результати
                ApplyLocalFilter();
            }

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

    // =============================  Імпорт  =============================
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

    // =============================  Дані  =============================
    private async Task ReloadAllAsync()
    {
        try
        {
            SetBusy(true);
            _all = [.. await PersonService.SearchAsync(null, _cts.Token)];
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося завантажити картки: {ex.Message}");
            _all = [];
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =============================  Дозволені статуси  =============================
    private async Task<IReadOnlyList<StatusKind>> BuildAllowedStatusesAsync(PersonStatus? current, CancellationToken ct)
    {
        var statuses = (_statuses?.Count > 0 ? _statuses : await StatusKindService.GetAllAsync(ct: ct)) ?? [];
        if (statuses.Count == 0) return [];

        static bool eq(string? a, string b) => !string.IsNullOrWhiteSpace(a) && a.Trim().Equals(b, StringComparison.OrdinalIgnoreCase);

        var inDistrict = statuses.FirstOrDefault(s => eq(s.Code, "30") || eq(s.Name, "В районі"));
        var inBr = statuses.FirstOrDefault(s => eq(s.Code, "100") || eq(s.Name, "В БР"));

        if (current is null || current.StatusKindId <= 0)
            return inDistrict is null ? Array.Empty<StatusKind>() : [inDistrict];

        var toIds = await StatusTransitionService.GetToIdsAsync(current.StatusKindId, ct) ?? [];
        if (inBr is not null) toIds.Remove(inBr.Id);

        return [.. statuses
            .Where(s => toIds.Contains(s.Id))
            .OrderBy(s => s.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
    }

    // =============================  Пошук/фільтр  =============================
    protected async Task OnSearchAsync()
    {
        ApplyLocalFilter();

        // QoL: один збіг — одразу відкриваємо модалку
        if (Filtered.Count == 1)
            await OpenSetStatus(Filtered[0]);
    }

    private static bool Has(string? haystack, string needle)
        => !string.IsNullOrWhiteSpace(haystack)
           && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private void ApplyLocalFilter()
    {
        Filtered.Clear();

        var s = (Search ?? string.Empty).Trim();
        if (s.Length == 0)
        {
            // zero-state: порожній список
            StateHasChanged();
            return;
        }

        foreach (var p in _all.Where(p =>
                   Has(p.FirstName, s) ||
                   Has(p.LastName, s) ||
                   Has(p.MiddleName, s) ||
                   Has(p.Rnokpp, s) ))
        {
            Filtered.Add(p);
        }

        StateHasChanged();
    }

    // =============================  Утиліти  =============================
    private void ToggleOneShotMode() => OneShotMode = !OneShotMode;

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    private static DateTime ToUtcFromLocalMidnight(DateTime localDateUnspecified)
    {
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
