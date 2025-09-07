//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Positions (code-behind)
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

public partial class PositionsPage : ComponentBase, IDisposable
{
    // =============== UI state ===============
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    protected PositionUnitViewModel? Selected { get; set; }

    private readonly CancellationTokenSource _cts = new();

    // Джерело даних (усі позиції з бекенда)
    private List<PositionUnit> _all = [];

    // Відфільтрований зріз (для експорту та службових задач)
    private List<PositionUnit> _filtered = [];

    // Те, що показуємо у таблиці (VM)
    protected ObservableCollection<PositionUnitViewModel> Items { get; private set; } = [];

    // =============== Modal ===============

    private PositionCreateModal? _createModal;
    private ConfirmModal? Confirm;

    // =============== DI ===============
    [Inject] protected IPositionService PositionService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] protected IValidator<CreatePositionUnitViewModel> CreateValidator { get; set; } = default!;

    // =============== Lifecycle ===============
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    // =============== Public handlers ===============
    /// <summary>Повне перезавантаження списку з бекенда з подальшою локальною рефільтрацією/сортом.</summary>
    protected async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            // 1) Забираємо усі позиції
            _all = [.. (await PositionService.GetPositionsAsync(onlyActive: false, _cts.Token))];

            // 2) Локальний фільтр/сорт/маппінг
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

    /// <summary>Викликається SearchBox після debounce. Тільки локальна рефільтрація.</summary>
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

    private async Task OnCreatedAsync(PositionUnitViewModel _)
    {
        // Після створення — просте повне перезавантаження списку
        await ReloadAsync();
    }

    /// <summary>Клік по чекбоксу активності: питаємо підтвердження перед оновленням.</summary>
    private async Task ToggleActiveAsync(PositionUnitViewModel vm, bool askConfirm = true)
    {
        if (Busy || vm is null) return;

        var newValue = !vm.IsActived;

        if (askConfirm && Confirm is not null)
        {
            var text = $"Змінити стан посади «{vm.ShortName}» на {(newValue ? "ШТАТНА" : "ЗАКРИТА")}?";
            var ok = await Confirm.ShowConfirmAsync(text);
            if (!ok) return; // користувач відмінив — нічого не міняємо
        }

        try
        {
            SetBusy(true);

            // бек може відхилити (виняток) — тоді чекбокс не змінюємо
            await PositionService.SetActiveStateAsync(vm.Id, newValue, _cts.Token);

            // успіх: оновлюємо тільки поточний VM, без повного Reload
            vm.IsActived = newValue;
            StateHasChanged();

            ToastService.ShowSuccess("Статус збережено.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Не вдалося зберегти: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =============== Import / Export ===============
    /// <summary>Обробка імпорту (валидація через FluentValidation + сервіс).</summary>
    protected async Task<ImportReportViewModel> ProcessImportAsync(IReadOnlyList<PositionUnit> rows)
    {
        try
        {
            SetBusy(true);
            // лише рахуємо/створюємо, без тостів
            return await PositionsUi.ImportAsync(rows, CreateValidator, PositionService, _cts.Token);
        }
        catch (Exception ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        finally
        {
            SetBusy(false);
        }
    }

    protected async void OnImportCompleted(ImportReportViewModel report)
    {
        if ((report.Errors?.Count ?? 0) > 0)
            ToastService.ShowWarning($"Імпорт завершено з помилками: {report.Errors!.Count}");
        else
            ToastService.ShowSuccess($"Імпорт успішний. Додано: {report.Added}, Оновлено: {report.Updated}");

        await ReloadAsync();
    }

    // =============== Private helpers ===============   

    /// <summary>Локальна фільтрація/сорт і маппінг у VM (через UIHelper), оновлює Items.</summary>
    private void ApplyFilterAndSort()
    {
        _filtered = [.. PositionsUi.Filter(_all, Search)];
        var mapped = PositionsUi.Transform(_filtered, null /* вже відфільтровано */);
        ResetItems(mapped);
    }

    private void ResetItems(IEnumerable<PositionUnitViewModel> items)
    {
        Items.Clear();
        foreach (var i in items) Items.Add(i);
        Selected = null;
        StateHasChanged();
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
