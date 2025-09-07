//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions (code-behind)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Components.Pages.StatusKids.Modals;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.StatusKids;

public partial class StatusTransitionsPage : ComponentBase
{
    // ---------------------------
    // [DI] Сервіси
    // ---------------------------
    [Inject] private IStatusKindService KindService { get; set; } = default!;
    [Inject] private IStatusTransitionService TransitionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // ---------------------------
    // [State] Локальний стан сторінки
    // ---------------------------
    private bool _busy;
    private List<StatusKind> _kinds = [];
    private int _selectedFromId;
    private string _currentFromName = "—";
    private string _currentFromCode = "—";

    // Дозволені переходи для поточного From
    private HashSet<int> _allowedToIds = [];

    // (Необов’язково) Для відладки/порівняння «до/після» — можна прибрати, якщо не потрібно
    private HashSet<int> _originalToIds = [];

    // ---------------------------
    // [Refs] Посилання на модальні вікна
    // ---------------------------
    private ConfirmModal? Confirm;
    private StatusCreateModal? _createModal;
    private StatusOrderEditModal? _orderModal;

    // ---------------------------
    // [Lifecycle] Ініціалізація
    // ---------------------------
    protected override async Task OnInitializedAsync()
    {
        await LoadKindsAsync();
        if (_kinds.Count > 0)
            await SelectFromAsync(_kinds[0].Id);
    }

    // ---------------------------
    // [Load] Завантаження списків
    // ---------------------------
    private async Task LoadKindsAsync()
    {
        _kinds = [.. await KindService.GetAllAsync(includeInactive: true)];
    }

    private async Task SelectFromAsync(int fromId)
    {
        if (_busy) return;

        _selectedFromId = fromId;

        var from = _kinds.FirstOrDefault(k => k.Id == fromId);
        if (from is null)
        {
            _currentFromName = "—";
            _currentFromCode = "—";
            _allowedToIds.Clear();
            _originalToIds.Clear();
            StateHasChanged();
            return;
        }

        _currentFromName = from.Name;
        _currentFromCode = from.Code;

        _busy = true;
        try
        {
            var toIds = await TransitionService.GetToIdsAsync(fromId);
            _allowedToIds = toIds;
            _allowedToIds.Remove(fromId); // сам-на-себе заборонено
            _originalToIds = [.. _allowedToIds];
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    // =========================================================================
    // [Commands - LEFT] Активність статусу (в лівій таблиці)
    // Використовується TransitionToggle: ConfirmActiveAsync + SaveActiveAsync + OnActiveCheckedChangedAsync
    // =========================================================================

    /// <summary>
    /// Підтвердження зміни активності статусу (ліва панель).
    /// </summary>
    private async Task<bool> ConfirmActiveAsync(int statusId, bool turnOn)
    {
        var name = _kinds.FirstOrDefault(x => x.Id == statusId)?.Name ?? $"#{statusId}";
        var text = $"Змінити активність «{name}» на {(turnOn ? "Активний" : "Неактивний")}?";
        return await (Confirm?.ShowConfirmAsync(text) ?? Task.FromResult(true));
    }

    /// <summary>
    /// Збереження активності статусу (ліва панель).
    /// </summary>
    private async Task SaveActiveAsync(int statusId, bool turnOn)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await KindService.SetActiveAsync(statusId, turnOn);
            Toast.ShowSuccess("Статус оновлено.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Помилка: {ex.Message}");
            throw; // Щоб TransitionToggle не міняв локальний стан при помилці
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Локальне оновлення прапорця активності після успішного збереження.
    /// </summary>
    private Task OnActiveCheckedChangedAsync(int statusId, bool newValue)
    {
        var item = _kinds.FirstOrDefault(x => x.Id == statusId);
        if (item is not null)
        {
            item.IsActive = newValue;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    // =========================================================================
    // [Commands - RIGHT] Дозволені переходи (права таблиця)
    // Використовується TransitionToggle: ConfirmAllowedAsync + SaveAllowedAsync + OnAllowedCheckedChangedAsync
    // =========================================================================

    /// <summary>
    /// Підтвердження зміни дозволу переходу (права панель).
    /// </summary>
    private async Task<bool> ConfirmAllowedAsync(int toId, bool turnOn)
    {
        var toName = _kinds.FirstOrDefault(x => x.Id == toId)?.Name ?? $"#{toId}";
        var action = turnOn ? "ДОДАТИ перехід до" : "ЗАБОРОНИТИ перехід до";
        return await (Confirm?.ShowConfirmAsync($"{action} «{toName}»?") ?? Task.FromResult(true));
    }

    /// <summary>
    /// Збереження нового набору дозволених переходів (права панель).
    /// </summary>
    private async Task SaveAllowedAsync(int toId, bool turnOn)
    {
        if (_busy || _selectedFromId == 0 || toId == _selectedFromId) return;

        // Готуємо новий набір на основі поточного стану
        var newAllowed = new HashSet<int>(_allowedToIds);
        if (turnOn) newAllowed.Add(toId);
        else newAllowed.Remove(toId);

        // Якщо змін фактично немає — не звертаємось до бекенда
        if (newAllowed.SetEquals(_allowedToIds)) return;

        _busy = true;
        try
        {
            await TransitionService.SaveAllowedAsync(_selectedFromId, newAllowed);
            _originalToIds = new HashSet<int>(newAllowed);
            Toast.ShowSuccess("Збережено.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Помилка збереження: {ex.Message}");
            throw; // Щоб TransitionToggle не міняв локальний стан при помилці
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Локальне оновлення набору _allowedToIds після успішного збереження.
    /// </summary>
    private Task OnAllowedCheckedChangedAsync(int toId, bool newValue)
    {
        if (_selectedFromId == 0 || toId == _selectedFromId) return Task.CompletedTask;

        if (newValue) _allowedToIds.Add(toId);
        else _allowedToIds.Remove(toId);

        StateHasChanged();
        return Task.CompletedTask;
    }

    // =========================================================================
    // [Commands - Create/Order] Створення статусу та редагування порядку
    // =========================================================================

    private void OpenCreateModal() => _createModal?.Open();

    private async Task OnCreatedAsync(StatusKind created)
    {
        await LoadKindsAsync();
        await SelectFromAsync(created.Id);
        Toast.ShowSuccess("Статус створено.");
    }

    private void OpenEditOrderModal(StatusKind k)
    {
        if (_busy) return;
        _orderModal?.Open(k.Id, k.Name, k.Order);
    }

    private void OnOrderSaved((int Id, int NewOrder) payload)
    {
        var (id, newOrder) = payload;

        var item = _kinds.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        item.Order = newOrder;

        _kinds = _kinds
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        StateHasChanged();
        Toast.ShowSuccess("Порядок оновлено.");
    }
}
