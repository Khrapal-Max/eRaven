//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Positions
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Components.Pages.Positions.Modals;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace eRaven.Components.Pages.Positions;

public partial class Positions : ComponentBase, IDisposable
{
    // =============== DI ===============
    [Inject] protected IPositionService PositionService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] private IValidator<CreatePositionUnitViewModel> CreateValidator { get; set; } = default!;

    // =============== UI state ===============
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    // Джерело даних (усі позиції з бекенда)
    private List<PositionUnit> _all = [];

    // Відфільтрований зріз (можеш використовувати для експорту тощо)
    private List<PositionUnit> _filtered = [];

    // Те, що показуємо у таблиці (VM)
    protected ObservableCollection<PositionUnitViewModel> Items { get; private set; } = [];

    protected PositionUnitViewModel? Selected { get; set; }
    protected ConfirmModal? Confirm;

    // внутрішній toggle
    private PositionUnitViewModel? _pendingToggle;
    private PositionCreateModal? _createModal;
    private bool _pendingNewValue;

    private readonly CancellationTokenSource _cts = new();

    // =============== Lifecycle ===============
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    // =============== Public handlers ===============
    /// <summary>Повне перезавантаження списку з бекенда з подальшою рефільтрацією.</summary>
    protected async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            // 1) Забираємо усі позиції
            _all = [.. (await PositionService.GetPositionsAsync(onlyActive: false, _cts.Token))];

            // 2) Застосовуємо поточний пошук
            ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Не вдалося завантажити позиції: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task OnCreatedAsync(PositionUnitViewModel vm)
    {
        await ReloadAsync();
    }

    /// <summary>Викликається SearchBox після debounce. Не тягне дані з бекенда — лише локальна рефільтрація.</summary>
    protected Task OnSearchAsync()
    {
        ApplyFilterAndSort();
        return Task.CompletedTask;
    }

    protected Task OnBusyChanged(bool busy)
    {
        SetBusy(busy);
        return Task.CompletedTask;
    }

    protected void OnRowClick(PositionUnitViewModel item) => Selected = item;

    protected Task CreateAsync()
    {
        _createModal?.Open();
        return Task.CompletedTask;
    }

    protected async Task AskToggleAsync(PositionUnitViewModel item)
    {
        if (Busy || Confirm is null) return;

        _pendingToggle = item;
        _pendingNewValue = !item.IsActived;

        var confirmText = $"Змінити стан посади «{item.ShortName}» на {(_pendingNewValue ? "ШТАТНА" : "ЗАКРИТА")}?";
        var confirmed = await Confirm.ShowConfirmAsync(confirmText);
        if (!confirmed)
        {
            // Відновлюємо вигляд (надійніше — просто повторна рефільтрація/перемаплення)
            ApplyFilterAndSort();
            return;
        }

        await ConfirmToggleAsync();
    }

    protected async Task<ImportReportViewModel> ProcessImportAsync(IReadOnlyList<PositionUnit> rows)
    {
        var added = 0;
        var updated = 0;
        var errors = new List<string>();

        try
        {
            ToastService.ShowInfo("Виконується імпорт…");
            SetBusy(true);

            // (опційно) локальна перевірка на дублікати кодів у самому файлі імпорту
            // щоб не ганяти зайві запити в БД, просто попередимо користувача
            var duplicateCodes = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Code))
                .Select(r => r.Code!.Trim())
                .GroupBy(c => c)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.Ordinal);

            if (duplicateCodes.Count > 0)
                errors.Add($"У файлі знайдено дублікати кодів: {string.Join(", ", duplicateCodes)}");

            foreach (var r in rows)
            {
                try
                {
                    // 1) мапимо рядок у ViewModel (бо валідатор — на VM)
                    var vm = new CreatePositionUnitViewModel
                    {
                        Code = (r.Code ?? string.Empty).Trim(),
                        ShortName = (r.ShortName ?? string.Empty).Trim(),
                        SpecialNumber = (r.SpecialNumber ?? string.Empty).Trim(),
                        OrgPath = (r.OrgPath ?? string.Empty).Trim(),
                    };

                    // 2) валідуємо (асинхронно, бо перевірка коду ходить у сервіс)
                    var result = await CreateValidator.ValidateAsync(vm, _cts.Token);
                    if (!result.IsValid)
                    {
                        var msg = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                        errors.Add($"{r.ShortName ?? "(без назви)"}: {msg}");
                        continue; // цей рядок не кидаємо в БД
                    }

                    // 3) якщо валідний — формуємо доменну сутність і створюємо
                    var entity = new PositionUnit
                    {
                        Code = string.IsNullOrWhiteSpace(vm.Code) ? null : vm.Code.Trim(),
                        ShortName = vm.ShortName.Trim(),
                        SpecialNumber = vm.SpecialNumber.Trim(),
                        OrgPath = string.IsNullOrWhiteSpace(vm.OrgPath) ? null : vm.OrgPath.Trim(),
                        IsActived = true
                    };

                    await PositionService.CreatePositionAsync(entity, _cts.Token);
                    added++;
                }
                catch (Exception exItem)
                {
                    errors.Add($"{r.ShortName ?? "(без назви)"}: {exItem.Message}");
                }
            }

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }

        return new ImportReportViewModel(added, updated, errors);
    }

    protected void OnImportCompleted(ImportReportViewModel report)
    {
        if ((report.Errors?.Count ?? 0) > 0)
            ToastService.ShowWarning($"Імпорт завершено з помилками: {report.Errors!.Count}");
        else
            ToastService.ShowSuccess($"Імпорт успішний. Додано: {report.Added}, Оновлено: {report.Updated}");
    }

    // =============== Private helpers ===============
    private async Task ConfirmToggleAsync()
    {
        if (_pendingToggle is null) return;

        try
        {
            SetBusy(true);
            await PositionService.SetActiveStateAsync(_pendingToggle.Id, _pendingNewValue, _cts.Token);
            ToastService.ShowSuccess("Статус збережено.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Не вдалося зберегти: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
            _pendingToggle = null;
            await ReloadAsync();
        }
    }

    /// <summary>Локальна фільтрація і сортування без звернення до бекенда. Результат — у Items.</summary>
    private void ApplyFilterAndSort()
    {
        // 1) ФІЛЬТР
        _filtered = [.. Filter(_all, Search)];

        // 2) СОРТ (мінімально — за Code; можна розширити як у SubDivision)
        var sorted = _filtered
            .OrderBy(p => p.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 3) МАППІНГ у VM і оновлення табличної колекції
        var mapped = sorted.Select(MapToVm);
        ResetItems(mapped);
    }

    private static IEnumerable<PositionUnit> Filter(IEnumerable<PositionUnit> src, string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return src;

        q = q.Trim();
        return src.Where(p =>
            (p.Code ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.ShortName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.SpecialNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (p.CurrentPerson?.FullName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    private static PositionUnitViewModel MapToVm(PositionUnit p) => new()
    {
        Id = p.Id,
        Code = p.Code ?? string.Empty,
        ShortName = p.ShortName,
        SpecialNumber = p.SpecialNumber,
        FullName = p.FullName,
        CurrentPersonFullName = p.CurrentPerson?.FullName,
        IsActived = p.IsActived
    };

    private void ResetItems(IEnumerable<PositionUnitViewModel> items)
    {
        Items.Clear();
        foreach (var i in items) Items.Add(i);
        Selected = null;
        StateHasChanged(); // м’який sync-рендер; ми на UI-потоці
    }

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    protected Task ResetSearch()
    {
        Search = null;
        ApplyFilterAndSort();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
