/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// RegisterStatuses (code-behind)
// - Дані: _all (повний набір), _view (відфільтрований вид)
// - Пошук локальний по ПІБ / ІПН / назві статусу
// - Перемикач стану інтервалу через TransitionToggle<Guid> + ConfirmModal
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Registers;

public partial class RegisterStatuses : ComponentBase, IDisposable
{
    // UI / DI
    [Inject] protected IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private ConfirmModal _confirm = default!;

    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    // Дані: _all — джерело істини; _view — поточний відфільтрований список
    private List<PersonStatus> _all = [];
    private IReadOnlyList<PersonStatus> _view = [];

    // -------------------- Життєвий цикл --------------------

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            var items = await PersonStatusService.GetAllAsync(_cts.Token);
            _all = [.. items];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося завантажити журнал: {ex.Message}");
            _all.Clear();
            _view = [];
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -------------------- Пошук/фільтр --------------------

    protected Task OnSearchAsync()
    {
        ApplyFilter();
        return Task.CompletedTask;
    }

    private void ApplyFilter()
    {
        var s = (Search ?? string.Empty).Trim();

        IEnumerable<PersonStatus> query = _all;

        if (s.Length > 0)
        {
            bool Has(string? src) => !string.IsNullOrWhiteSpace(src) &&
                                     src.Contains(s, StringComparison.OrdinalIgnoreCase);

            query = query.Where(x =>
                Has(x.Person?.FirstName) ||
                Has(x.Person?.LastName) ||
                Has(x.Person?.MiddleName) ||
                Has(x.Person?.Rnokpp) ||
                Has(x.StatusKind?.Name));
        }

        // Сортування за замовчуванням: найсвіжіші першими
        _view = query
           .OrderByDescending(x => x.OpenDate)
           .ThenByDescending(x => x.Modified)
           .ToList()
           .AsReadOnly();

        StateHasChanged();
    }

    // -------------------- Тумблер стану --------------------

    private async Task<bool> ConfirmToggleAsync(Guid statusId, bool turnOn)
    {
        var text = turnOn
            ? "Позначити запис як АКТИВНИЙ?"
            : "Позначити запис як НЕАКТИВНИЙ?";
        return await _confirm.ShowConfirmAsync(text);
    }

    private async Task SaveToggleAsync(Guid statusId, bool turnOn)
    {
        try
        {
            SetBusy(true);

            // Сервіс робить «toggle». Тут це доречно: ми викликаємо рівно один раз.
            var newState = await PersonStatusService.UpdateStateIsActive(statusId, _cts.Token);

            // Синхронізуємо локальну модель (на випадок, якщо елемент не візьмуть із CheckedChanged)
            var item = _all.FirstOrDefault(x => x.Id == statusId);
            if (item is not null) item.IsActive = newState;

            // Можна підсвітити успіх
            Toast.ShowSuccess(newState ? "Інтервал активовано." : "Інтервал деактивовано.");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося змінити стан: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private Task OnToggleChangedAsync(PersonStatus row, bool isActive)
    {
        // Керований компонент: фіксуємо нове значення у відображуваній моделі
        row.IsActive = isActive;
        StateHasChanged();
        return Task.CompletedTask;
    }

    // -------------------- Службові --------------------

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