/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// StatusTransitionsPage (code-behind)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Domain.Exceptions;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace eRaven.Components.Pages.Statuses;

public partial class StatusesPage : ComponentBase, IDisposable
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
    [Inject] private IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private PersonApplicationService PersonAppService { get; set; } = default!;

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

            var openUtc = StatusesUi.ToUtcFromLocalMidnight(vm.Moment);

            // ✅ НОВИЙ спосіб - вся логіка в агрегаті
            await PersonAppService.ChangePersonStatusAsync(
                personId: vm.PersonId,
                newStatusKindId: vm.StatusId,
                effectiveAtUtc: openUtc,
                note: vm.Note?.Trim(),
                author: vm.Author?.Trim()
            );

            Toast.ShowSuccess("Статус збережено.");
            await ReloadAllAsync();
        }
        catch (DomainException ex)
        {
            // Доменні помилки - показуємо користувачу
            Toast.ShowError(ex.Message);
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
    protected async Task<ImportReportViewModel> ProcessImportedStatusesAsync(
    IReadOnlyList<PersonStatusImportView> rows)
    {
        Toast.ShowInfo("Виконується імпорт...");

        int added = 0;
        var errors = new List<string>();

        if (rows is null || rows.Count == 0)
            return new ImportReportViewModel(0, 0, errors);

        // На всяк випадок — щоб мати актуальний кеш осіб
        if (_all.Count == 0)
            await ReloadAllAsync();

        foreach (var (row, idx) in rows.Select((r, i) => (r, i: i + 2))) // +2: заголовок + 1-based
        {
            // 1) Пропускаємо, якщо обов'язкові поля порожні
            var rnokpp = row.Rnokpp?.Trim();
            var kindId = row.StatusKindId ?? 0;
            var dateLocal = row.FromDateLocal; // припускаємо, що це DateTime? в твоїй моделі

            if (string.IsNullOrWhiteSpace(rnokpp) || kindId <= 0 || dateLocal == default)
            {
                // Просто скіпаємо рядок (додаю інфо в звіт, щоб було видно, що пропущено)
                errors.Add($"Row {idx}: skipped (missing RNOKPP/StatusKindId/FromDateLocal).");
                continue;
            }

            // 2) Знаходимо особу по RNOKPP (у завантаженому списку)
            var person = _all.FirstOrDefault(p =>
                string.Equals(p.Rnokpp?.Trim(), rnokpp, StringComparison.OrdinalIgnoreCase));

            if (person is null)
            {
                errors.Add($"Row {idx}: person with RNOKPP '{rnokpp}' not found.");
                continue;
            }

            try
            {
                // 3) 00:00 локального дня -> UTC
                var localUnspec = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
                var openUtc = StatusesUi.ToUtcFromLocalMidnight(localUnspec);

                // 4) Формуємо інтервал та віддаємо сервісу
                var ps = new PersonStatus
                {
                    Id = Guid.Empty,
                    PersonId = person.Id,
                    StatusKindId = kindId,
                    OpenDate = openUtc,
                    IsActive = true,
                    Note = string.IsNullOrWhiteSpace(row.Note) ? null : row.Note.Trim(),
                    Author = "import",
                    Modified = DateTime.UtcNow
                };

                await PersonStatusService.SetStatusAsync(ps, _cts.Token);
                added++;
            }
            catch (Exception ex)
            {
                // Будь-яка бізнес-валідація (перетини/заборонений перехід тощо)
                errors.Add($"Row {idx}: {ex.Message}");
            }
        }

        Toast.ShowInfo("Імпорт закінчено.");

        await ReloadAllAsync(); // оновимо список після імпорту
        return new ImportReportViewModel(Added: added, Updated: 0, Errors: errors);
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

        static bool eq(string? a, string b)
            => !string.IsNullOrWhiteSpace(a) && a.Trim().Equals(b, StringComparison.OrdinalIgnoreCase);

        // спец-статуси
        var inDistrict = statuses.FirstOrDefault(s => eq(s.Code, "30") || eq(s.Name, "В районі"));
        var inBr = statuses.FirstOrDefault(s => eq(s.Code, "100") || eq(s.Name, "В БР"));

        // перший статус — тільки «В районі»
        if (current is null || current.StatusKindId <= 0)
            return inDistrict is null ? Array.Empty<StatusKind>() : [inDistrict];

        // стандартна карта переходів
        var toIds = await StatusTransitionService.GetToIdsAsync(current.StatusKindId, ct) ?? [];
        if (toIds.Count == 0) return [];

        // ніколи не пропонуємо «В БР»
        if (inBr is not null) toIds.Remove(inBr.Id);

        // НОВЕ ПРАВИЛО: якщо поточний = «В БР», не показуємо «В районі» (перехід лише по наказу)
        if (inDistrict is not null && inBr is not null && current.StatusKindId == inBr.Id)
            toIds.Remove(inDistrict.Id);

        // фінальний перелік
        return [.. statuses
            .Where(s => toIds.Contains(s.Id))
            .OrderBy(s => s.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)];
    }

    // =============================  Правило “зняти з посади” (по Code)  =============================
    private static bool IsUnassignCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        // нормалізуємо (обрізаємо пробіли; без урахування регістру)
        var c = code.Trim();

        // Покриваємо «РОЗПОР», «нб» (кирилиця). За потреби тут додати інші службові коди.
        return c.Equals("РОЗПОР", StringComparison.OrdinalIgnoreCase)
            || c.Equals("нб", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Чи треба знімати з посади з огляду на обраний статус (Id → Code).
    /// Дефенсивно гарантуємо наявність `_statuses`.
    /// </summary>
    private async Task<bool> RequiresUnassignByCodeAsync(int statusId, CancellationToken ct)
    {
        if (_statuses is null || _statuses.Count == 0)
            _statuses = await StatusKindService.GetAllAsync(ct: ct);

        var sk = _statuses?.FirstOrDefault(s => s.Id == statusId);
        return IsUnassignCode(sk?.Code);
    }

    // =============================  Пошук/фільтр  =============================
    protected async Task OnSearchAsync()
    {
        ApplyLocalFilter();

        // QoL: один збіг — одразу відкриваємо модалку
        if (Filtered.Count == 1)
            await OpenSetStatus(Filtered[0]);
    }

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

        foreach (var p in StatusesUi.FilterPersons(_all, s))
            Filtered.Add(p);

        StateHasChanged();
    }

    // =============================  Утиліти  =============================
    private void ToggleOneShotMode() => OneShotMode = !OneShotMode;

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
*/