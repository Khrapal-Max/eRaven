//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions (code-behind) — managment statuses
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.ConfirmService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Components.Pages.StatusTransitions.Modals;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.StatusTransitions;

public partial class StatusTransitionsPage : ComponentBase
{
    //=====================================================================
    // [DI]
    //=====================================================================
    [Inject] private IConfirmService ConfirmService { get; set; } = default!;
    [Inject] private IStatusKindService KindService { get; set; } = default!;
    [Inject] private IStatusTransitionService TransitionService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    //=====================================================================
    // [State]
    //=====================================================================
    private bool _busy;
    private List<StatusKind> _kinds = [];
    private Dictionary<int, StatusKind> _kindsById = [];

    // Ліва таблиця: керований вибір
    private StatusKind? _selectedFrom;     // ← TableBaseComponent.SelectedItem
    private int _selectedFromId;
    private string _currentFromName = "—";
    private string _currentFromCode = "—";

    // Права таблиця: дозволені переходи
    private HashSet<int> _allowedToIds = [];

    //=====================================================================
    // [Refs]
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

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Confirm is not null)
            ConfirmService.Use(Confirm.ShowConfirmAsync);
    }

    //=====================================================================
    // [Load & Select]
    //=====================================================================
    private async Task LoadKindsAsync()
    {
        _kinds = [.. await KindService.GetAllAsync(includeInactive: true)];
        _kindsById = _kinds.ToDictionary(k => k.Id);
    }

    private async Task SelectFromAsync(int fromId)
    {
        if (_busy) return;

        _selectedFromId = fromId;
        _selectedFrom = _kindsById.GetValueOrDefault(fromId);

        if (_selectedFrom is null)
        {
            _currentFromName = "—";
            _currentFromCode = "—";
            _allowedToIds.Clear();
            StateHasChanged();
            return;
        }

        _currentFromName = _selectedFrom.Name;
        _currentFromCode = _selectedFrom.Code;

        await WithBusyAsync(async () =>
        {
            var toIds = await TransitionService.GetToIdsAsync(fromId);
            _allowedToIds = toIds;
            _allowedToIds.Remove(fromId); // self заборонено
        });
    }

    // Викликається від TableBaseComponent.SelectedItemChanged
    private async Task OnLeftSelectedChangedAsync(StatusKind? item)
    {
        if (item is null) return;
        await SelectFromAsync(item.Id);
    }

    // Опціонально: якщо треба обробляти сам клік (не лише зміну Selection)
    private Task OnLeftRowClick(StatusKind item) => Task.CompletedTask;

    //=====================================================================
    // [LEFT] — Активність
    //=====================================================================
    private async Task<bool> ConfirmActiveAsync(int statusId, bool turnOn)
    {
        var name = _kindsById.TryGetValue(statusId, out var k) ? k.Name : $"#{statusId}";
        var text = $"Змінити активність «{name}» на {(turnOn ? "Активний" : "Неактивний")}?";
        return await ConfirmService.AskAsync(text);
    }

    private async Task SaveActiveAsync(int statusId, bool turnOn)
    {
        if (_busy) return;

        await WithBusyAsync(async () =>
        {
            var ok = await KindService.SetActiveAsync(statusId, turnOn);
            if (ok) ToastService.ShowSuccess("Статус оновлено.");
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
        var toName = _kindsById.TryGetValue(toId, out var k) ? k.Name : $"#{toId}";
        var action = turnOn ? "ДОДАТИ перехід до" : "ЗАБОРОНИТИ перехід до";
        return await ConfirmService.AskAsync($"{action} «{toName}»?");
    }

    private async Task SaveAllowedAsync(int toId, bool turnOn)
    {
        if (_busy || _selectedFromId == 0 || toId == _selectedFromId) return;

        var (changed, newAllowed) = BuildNewAllowedSet(_allowedToIds, toId, turnOn);
        if (!changed) return;

        await WithBusyAsync(async () =>
        {
            await TransitionService.SaveAllowedAsync(_selectedFromId, newAllowed);
            ToastService.ShowSuccess("Збережено.");
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
    // [Create/Order]
    //=====================================================================
    private void OpenCreateModal() => _createModal?.Open();

    private async Task OnCreatedAsync(StatusKind created)
    {
        await LoadKindsAsync();
        await SelectFromAsync(created.Id);
        ToastService.ShowSuccess("Статус створено.");
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
            item.Order = newOrder;

        _kinds = _kinds
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        _kindsById = _kinds.ToDictionary(k => k.Id);
        StateHasChanged();
        ToastService.ShowSuccess("Порядок оновлено.");
    }

    //=====================================================================
    // [Helpers]
    //=====================================================================
    private async Task WithBusyAsync(Func<Task> action)
    {
        _busy = true;
        try { await action(); }
        finally { _busy = false; StateHasChanged(); }
    }

    private static (bool changed, HashSet<int> newAllowed) BuildNewAllowedSet(HashSet<int> current, int toId, bool turnOn)
    {
        var newAllowed = new HashSet<int>(current);
        if (turnOn) newAllowed.Add(toId);
        else newAllowed.Remove(toId);
        return (!newAllowed.SetEquals(current), newAllowed);
    }
}
