//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// RegisterStatuses (code-behind)
// - Дані: _all (повний набір), _view (відфільтрований вид)
// - Пошук локальний по ПІБ / ІПН / назві статусу
// - Перемикач стану інтервалу через TransitionToggle<Guid> + ConfirmModal
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace eRaven.Components.Pages.Registers;

public partial class RegisterStatuses : ComponentBase, IDisposable
{
    // UI / DI
    [Inject] protected IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;
    [Inject] protected ILogger<RegisterStatuses> Logger { get; set; } = default!;

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
            await SetBusyAsync(true);

            var items = await PersonStatusService.GetAllAsync(_cts.Token);
            _all = [.. items];
            await ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити журнал"))
            {
                throw;
            }
            _all.Clear();
            if (_view.Count > 0)
            {
                _view = [];
                await InvokeAsync(StateHasChanged);
            }
        }
        finally
        {
            await SetBusyAsync(false);
        }
    }

    // -------------------- Пошук/фільтр --------------------

    protected Task OnSearchAsync() => ApplyFilterAsync();

    private async Task ApplyFilterAsync()
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
        var next = query
           .OrderByDescending(x => x.OpenDate)
           .ThenByDescending(x => x.Modified)
           .ToList()
           .AsReadOnly();

        if (SameOrder(_view, next))
        {
            return;
        }

        _view = next;
        await InvokeAsync(StateHasChanged);
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
            await SetBusyAsync(true);

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
            if (!TryHandleKnownException(ex, "Не вдалося змінити стан"))
            {
                throw;
            }
        }
        finally
        {
            await SetBusyAsync(false);
        }
    }

    private Task OnToggleChangedAsync(PersonStatus row, bool isActive)
    {
        // Керований компонент: фіксуємо нове значення у відображуваній моделі
        if (row.IsActive == isActive)
        {
            return Task.CompletedTask;
        }

        row.IsActive = isActive;
        return InvokeAsync(StateHasChanged);
    }

    // -------------------- Службові --------------------

    private async Task SetBusyAsync(bool value)
    {
        if (Busy == value)
        {
            return;
        }

        Busy = value;
        await InvokeAsync(StateHasChanged);
    }

    private static bool SameOrder(IReadOnlyList<PersonStatus> current, IReadOnlyList<PersonStatus> next)
    {
        if (ReferenceEquals(current, next)) return true;
        if (current.Count != next.Count) return false;

        for (var i = 0; i < current.Count; i++)
        {
            var a = current[i];
            var b = next[i];
            if ((a?.Id ?? Guid.Empty) != (b?.Id ?? Guid.Empty))
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
                Toast.ShowError($"{message}: {ex.Message}");
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
                Toast.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
