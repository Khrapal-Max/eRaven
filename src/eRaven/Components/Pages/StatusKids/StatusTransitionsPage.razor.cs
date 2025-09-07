//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions (code-behind) — cleaned & grouped
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
    //=====================================================================
    // [DI]
    //=====================================================================
    [Inject] private IStatusKindService KindService { get; set; } = default!;
    [Inject] private IStatusTransitionService TransitionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    //=====================================================================
    // [State] — сторінковий стан та кеш
    //=====================================================================
    private bool _busy;
    private List<StatusKind> _kinds = [];
    private Dictionary<int, StatusKind> _kindsById = [];  // 🔹 швидкі пошуки по Id

    private int _selectedFromId;
    private string _currentFromName = "—";
    private string _currentFromCode = "—";

    // Дозволені переходи для поточного From
    private HashSet<int> _allowedToIds = [];

    //=====================================================================
    // [Refs] — модалки
    //=====================================================================
    private ConfirmModal? Confirm;
    private StatusCreateModal? _createModal;
    private StatusOrderEditModal? _orderModal;

    //=====================================================================
    // [Lifecycle]
    //=====================================================================
    protected override async Task OnInitializedAsync()
    {
        await LoadKindsAsync();
        if (_kinds.Count > 0)
            await SelectFromAsync(_kinds[0].Id);
    }

    //=====================================================================
    // [Load] — завантаження та вибір "From"
    //=====================================================================
    private async Task LoadKindsAsync()
    {
        _kinds = [.. await KindService.GetAllAsync(includeInactive: true)];
        _kindsById = _kinds.ToDictionary(k => k.Id);  // 🔹 оновлюємо кеш
    }

    private async Task SelectFromAsync(int fromId)
    {
        if (_busy) return;

        _selectedFromId = fromId;

        if (!_kindsById.TryGetValue(fromId, out var from))
        {
            _currentFromName = "—";
            _currentFromCode = "—";
            _allowedToIds.Clear();
            StateHasChanged();
            return;
        }

        _currentFromName = from.Name;
        _currentFromCode = from.Code;

        await WithBusyAsync(async () =>
        {
            var toIds = await TransitionService.GetToIdsAsync(fromId);
            _allowedToIds = toIds;
            _allowedToIds.Remove(fromId); // 🔸 сам-на-себе заборонено
        });
    }

    //=====================================================================
    // [LEFT] — Активність статусу
    //=====================================================================
    private async Task<bool> ConfirmActiveAsync(int statusId, bool turnOn)
    {
        var name = GetKindName(statusId);
        var text = $"Змінити активність «{name}» на {(turnOn ? "Активний" : "Неактивний")}?";
        return await (Confirm?.ShowConfirmAsync(text) ?? Task.FromResult(true));
    }

    private async Task SaveActiveAsync(int statusId, bool turnOn)
    {
        if (_busy) return;

        await WithBusyAsync(async () =>
        {
            await KindService.SetActiveAsync(statusId, turnOn);
            Toast.ShowSuccess("Статус оновлено.");
        });
    }

    private Task OnActiveCheckedChangedAsync(int statusId, bool newValue)
    {
        if (_kindsById.TryGetValue(statusId, out var item))
        {
            item.IsActive = newValue;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    //=====================================================================
    // [RIGHT] — Дозволені переходи
    //=====================================================================
    private async Task<bool> ConfirmAllowedAsync(int toId, bool turnOn)
    {
        var toName = GetKindName(toId);
        var action = turnOn ? "ДОДАТИ перехід до" : "ЗАБОРОНИТИ перехід до";
        return await (Confirm?.ShowConfirmAsync($"{action} «{toName}»?") ?? Task.FromResult(true));
    }

    private async Task SaveAllowedAsync(int toId, bool turnOn)
    {
        if (_busy || _selectedFromId == 0 || toId == _selectedFromId) return;

        var (changed, newAllowed) = BuildNewAllowedSet(_allowedToIds, toId, turnOn);
        if (!changed) return;

        await WithBusyAsync(async () =>
        {
            await TransitionService.SaveAllowedAsync(_selectedFromId, newAllowed);
            Toast.ShowSuccess("Збережено.");
        });
    }

    private Task OnAllowedCheckedChangedAsync(int toId, bool newValue)
    {
        if (_selectedFromId == 0 || toId == _selectedFromId) return Task.CompletedTask;

        if (newValue) _allowedToIds.Add(toId);
        else _allowedToIds.Remove(toId);

        StateHasChanged();
        return Task.CompletedTask;
    }

    //=====================================================================
    // [Create/Order] — модалки створення та редагування порядку
    //=====================================================================
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

        if (_kindsById.TryGetValue(id, out var item))
        {
            item.Order = newOrder;
        }

        // Перевпорядкування (Order, Name)
        _kinds = _kinds
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        _kindsById = _kinds.ToDictionary(k => k.Id); // 🔹 тримаємо кеш у синхроні

        StateHasChanged();
        Toast.ShowSuccess("Порядок оновлено.");
    }

    //=====================================================================
    // [Helpers] — локальні утиліти для чистого коду
    //=====================================================================

    /// <summary>Уніфікований шаблон busy-виконання з безпечною зміною стану.</summary>
    private async Task WithBusyAsync(Func<Task> action)
    {
        _busy = true;
        try { await action(); }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    /// <summary>Ім'я статусу за Id (fallback: #id).</summary>
    private string GetKindName(int id)
        => _kindsById.TryGetValue(id, out var k) ? k.Name : $"#{id}";

    /// <summary>Побудувати новий набір дозволів, повернути «чи є зміна» та сам набір.</summary>
    private static (bool changed, HashSet<int> newAllowed) BuildNewAllowedSet(HashSet<int> current, int toId, bool turnOn)
    {
        var newAllowed = new HashSet<int>(current);
        if (turnOn) newAllowed.Add(toId);
        else newAllowed.Remove(toId);

        var changed = !newAllowed.SetEquals(current);
        return (changed, newAllowed);
    }
}
