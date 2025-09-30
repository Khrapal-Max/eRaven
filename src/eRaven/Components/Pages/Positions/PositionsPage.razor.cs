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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace eRaven.Components.Pages.Positions;

public partial class PositionsPage : ComponentBase, IDisposable
{
    // =========================
    // [UI state] візуальний стан
    // =========================
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }
    protected PositionUnitViewModel? Selected { get; set; }

    // =========================
    // [Infra] ресурси й токени
    // =========================
    private readonly CancellationTokenSource _cts = new();

    // =========================
    // [Data] джерела та представлення
    // =========================
    private List<PositionUnit> _all = [];                      // усі позиції з бекенда
    private List<PositionUnit> _filtered = [];                 // відфільтрований зріз
    protected ObservableCollection<PositionUnitViewModel> Items { get; private set; } = []; // для таблиці

    // =========================
    // [Modals] посилання
    // =========================
    private PositionCreateModal? _createModal;
    private ConfirmModal? Confirm;

    // =========================
    // [DI] сервіси
    // =========================
    [Inject] protected IPositionService PositionService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] protected IValidator<CreatePositionUnitViewModel> CreateValidator { get; set; } = default!;
    [Inject] protected ILogger<PositionsPage> Logger { get; set; } = default!;

    // =========================
    // [Lifecycle]
    // =========================
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    // =========================
    // [Public handlers]
    // =========================
    /// <summary>Повне перезавантаження списку з бекенда з подальшою локальною рефільтрацією/сортом.</summary>
    protected async Task ReloadAsync()
    {
        try
        {
            await SetBusyAsync(true);

            // 1) Забираємо усі позиції
            _all = [.. (await PositionService.GetPositionsAsync(onlyActive: false, _cts.Token))];

            // 2) Локальний фільтр/сорт/маппінг
            await ApplyFilterAndSortAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити позиції"))
            {
                throw;
            }
        }
        finally
        {
            await SetBusyAsync(false);
        }
    }

    /// <summary>Викликається SearchBox після debounce. Тільки локальна рефільтрація.</summary>
    protected Task OnSearchAsync() => ApplyFilterAndSortAsync();

    protected Task OnBusyChanged(bool busy) => SetBusyAsync(busy);

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

    // =========================================================================
    // [Active Toggle] — керований чекбокс через TransitionToggle
    // 1) ConfirmPositionActiveAsync  — текст підтвердження
    // 2) SavePositionActiveAsync     — виклик бекенда; на помилці кидаємо далі
    // 3) OnPositionActiveCheckedChangedAsync — локальне оновлення VM після успіху
    // =========================================================================

    /// <summary>Підтвердження зміни активності посади.</summary>
    private async Task<bool> ConfirmPositionActiveAsync(Guid positionId, bool turnOn)
    {
        var vm = Items.FirstOrDefault(x => x.Id == positionId);
        var name = vm?.ShortName ?? $"#{positionId}";
        var text = $"Змінити стан посади «{name}» на {(turnOn ? "ШТАТНА" : "ЗАКРИТА")}?";
        return await (Confirm?.ShowConfirmAsync(text) ?? Task.FromResult(true));
    }

    /// <summary>Збереження активності в бекенді (кидає далі при помилці, щоб TransitionToggle не оновлював локальний стан).</summary>
    private async Task SavePositionActiveAsync(Guid positionId, bool turnOn)
    {
        if (Busy) return;
        await SetBusyAsync(true);
        try
        {
            await PositionService.SetActiveStateAsync(positionId, turnOn, _cts.Token);
            ToastService.ShowSuccess("Статус збережено.");
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося зберегти"))
            {
                throw;
            }
        }
        finally
        {
            await SetBusyAsync(false);
        }
    }

    /// <summary>Локальне оновлення VM після успішного збереження.</summary>
    private Task OnPositionActiveCheckedChangedAsync(Guid positionId, bool newValue)
    {
        var vm = Items.FirstOrDefault(x => x.Id == positionId);
        if (vm is not null)
        {
            if (vm.IsActived == newValue)
            {
                return Task.CompletedTask;
            }

            vm.IsActived = newValue;
            return InvokeAsync(StateHasChanged);
        }
        return Task.CompletedTask;
    }

    // =========================
    // [Import / Export]
    // =========================
    /// <summary>Обробка імпорту (валидація через FluentValidation + сервіс).</summary>
    protected async Task<ImportReportViewModel> ProcessImportAsync(IReadOnlyList<PositionUnit> rows)
    {
        ToastService.ShowInfo("Виконується імпорт");

        try
        {
            await SetBusyAsync(true);
            // лише рахуємо/створюємо, без тостів
            return await PositionsUi.ImportAsync(rows, CreateValidator, PositionService, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FluentValidation.ValidationException ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        catch (InvalidOperationException ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        catch (ArgumentException ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        catch (HttpRequestException ex)
        {
            return new ImportReportViewModel(0, 0, [ex.Message]);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during positions import");
            throw;
        }
        finally
        {
            await SetBusyAsync(false);
        }
    }

    protected async Task OnImportCompleted(ImportReportViewModel report)
    {
        if ((report.Errors?.Count ?? 0) > 0)
            ToastService.ShowWarning($"Імпорт завершено з помилками: {report.Errors!.Count}");
        else
            ToastService.ShowSuccess($"Імпорт успішний. Додано: {report.Added}, Оновлено: {report.Updated}");

        await ReloadAsync();
    }

    // =========================
    // [Helpers]
    // =========================
    /// <summary>Локальна фільтрація/сорт і маппінг у VM (через UIHelper), оновлює Items.</summary>
    private async Task ApplyFilterAndSortAsync()
    {
        var nextFiltered = [.. PositionsUi.Filter(_all, Search)];
        var filteredChanged = !SameUnits(_filtered, nextFiltered);
        _filtered = nextFiltered;

        var nextItems = PositionsUi.Transform(_filtered, null /* вже відфільтровано */).ToList();
        var itemsChanged = !SameViewModels(Items, nextItems);
        var hadSelection = Selected is not null;

        if (!filteredChanged && !itemsChanged && !hadSelection)
        {
            return;
        }

        if (itemsChanged)
        {
            Items.Clear();
            foreach (var vm in nextItems)
            {
                Items.Add(vm);
            }
        }

        if (hadSelection)
        {
            Selected = null;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetBusyAsync(bool value)
    {
        if (Busy == value)
        {
            return;
        }

        Busy = value;
        await InvokeAsync(StateHasChanged);
    }

    private static bool SameUnits(IReadOnlyList<PositionUnit> current, IReadOnlyList<PositionUnit> next)
    {
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].Id != next[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameViewModels(IReadOnlyList<PositionUnitViewModel> current, IReadOnlyList<PositionUnitViewModel> next)
    {
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            var a = current[i];
            var b = next[i];

            if (a.Id != b.Id)
            {
                return false;
            }

            if (!string.Equals(a.Code, b.Code, StringComparison.Ordinal) ||
                !string.Equals(a.ShortName, b.ShortName, StringComparison.Ordinal) ||
                !string.Equals(a.SpecialNumber, b.SpecialNumber, StringComparison.Ordinal) ||
                !string.Equals(a.FullName, b.FullName, StringComparison.Ordinal) ||
                !string.Equals(a.CurrentPersonFullName, b.CurrentPersonFullName, StringComparison.Ordinal) ||
                a.IsActived != b.IsActived)
            {
                return false;
            }
        }

        return true;
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
                ToastService.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
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
                ToastService.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    protected Task ResetSearch()
    {
        Search = null;
        return ApplyFilterAndSortAsync();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
