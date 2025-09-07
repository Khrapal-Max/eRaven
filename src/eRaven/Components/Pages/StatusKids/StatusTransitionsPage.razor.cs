//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitions
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
    [Inject] private IStatusKindService KindService { get; set; } = default!;
    [Inject] private IStatusTransitionService TransitionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private bool _busy;
    private List<StatusKind> _kinds = [];
    private int _selectedFromId;
    private string _currentFromName = "—";
    private string _currentFromCode = "—";
    private HashSet<int> _allowedToIds = [];
    private HashSet<int> _originalToIds = [];

    private ConfirmModal? Confirm;
    private StatusCreateModal? _createModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadKindsAsync();
        if (_kinds.Count > 0)
            await SelectFromAsync(_kinds[0].Id);
    }

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
            _allowedToIds.Remove(fromId); // self-loop заборонено
            _originalToIds = [.. _allowedToIds];
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private void ToggleTo(int toId, bool on)
    {
        if (_selectedFromId == 0 || toId == _selectedFromId) return;
        if (on) _allowedToIds.Add(toId);
        else _allowedToIds.Remove(toId);
    }

    private async Task ToggleActiveAsync(StatusKind k)
    {
        if (_busy) return;

        var newValue = !k.IsActive;
        if (Confirm is not null)
        {
            var ok = await Confirm.ShowConfirmAsync($"Змінити активність «{k.Name}» на {(newValue ? "Активний" : "Неактивний")}?");
            if (!ok) return;
        }

        _busy = true;
        try
        {
            await KindService.SetActiveAsync(k.Id, newValue);
            k.IsActive = newValue;
            StateHasChanged();
            Toast.ShowSuccess("Статус оновлено.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Помилка: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ToggleToAsync(int toId, bool turnOn)
    {
        if (_busy || _selectedFromId == 0 || toId == _selectedFromId)
        {
            // сам-на-себе, або зайнято — просто перемалюємо, щоб повернути візуальний стан
            StateHasChanged();
            return;
        }

        // Підготовка нового набору без мутації поточного
        var newAllowed = new HashSet<int>(_allowedToIds);
        if (turnOn) newAllowed.Add(toId);
        else newAllowed.Remove(toId);

        // Якщо фактичної зміни немає — нічого не робимо
        if (newAllowed.SetEquals(_allowedToIds))
        {
            StateHasChanged();
            return;
        }

        // Підтвердження
        if (Confirm is not null)
        {
            var toName = _kinds.FirstOrDefault(x => x.Id == toId)?.Name ?? $"#{toId}";
            var action = turnOn ? "ДОДАТИ перехід до" : "ЗАБОРОНИТИ перехід до";
            var ok = await Confirm.ShowConfirmAsync($"{action} «{toName}»?");
            if (!ok)
            {
                // користувач відмінив — повернути чекбокс назад
                StateHasChanged();
                return;
            }
        }

        // Збереження
        _busy = true;
        try
        {
            await TransitionService.SaveAllowedAsync(_selectedFromId, newAllowed);
            _allowedToIds = newAllowed;          // приймаємо новий стан
            _originalToIds = new HashSet<int>(_allowedToIds); // синхронізуємо оригінал
            Toast.ShowSuccess("Збережено.");
        }
        catch (Exception ex)
        {
            // помилка — не змінюємо локальний стан
            Toast.ShowError($"Помилка збереження: {ex.Message}");
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    private void OpenCreateModal() => _createModal?.Open();

    private async Task OnCreatedAsync(StatusKind created)
    {
        await LoadKindsAsync();
        await SelectFromAsync(created.Id);
        Toast.ShowSuccess("Статус створено.");
    }
}
