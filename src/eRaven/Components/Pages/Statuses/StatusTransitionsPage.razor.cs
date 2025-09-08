//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Components.Pages.Statuses.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace eRaven.Components.Pages.Statuses;

public partial class StatusTransitionsPage : ComponentBase, IDisposable
{
    // ========= DI =========
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // ========= UI state =========
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private CancellationTokenSource _cts = new();

    // Джерело даних
    private List<Person> _all = [];
    protected ObservableCollection<Person> Items { get; private set; } = [];

    // ========= Modals =========
    private StatusSetModal? _setStatusModal;

    // ========= Actions =========
    protected async Task OnSearchAsync()
    {
        if (_all.Count == 0)
        {
            try
            {
                SetBusy(true);
                _all = [.. await PersonService.SearchAsync(null, _cts.Token)];
            }
            catch (Exception ex)
            {
                _all.Clear();
                ResetItems([]);
                Toast.ShowError($"Помилка завантаження: {ex.Message}");
                return;
            }
            finally { SetBusy(false); }
        }

        ApplyFilter();
    }

    protected void OpenSetStatus(Person p) => _setStatusModal?.Open(p);

    // ========= Refresh after status change =========

    private async Task OnStatusChangedAsync(Guid personId)
    {
        // Можемо обрати один з підходів: точкове оновлення або full reload.
        // 1) Точкове оновлення (швидко й дешево):
        await RefreshPersonAsync(personId);

        // 2) Альтернатива для масових змін:
        // await ReloadAllAsync();
    }

    private async Task RefreshPersonAsync(Guid personId)
    {
        try
        {
            // підтягуємо оновлену картку (з навігаціями)
            var fresh = await PersonService.GetByIdAsync(personId, _cts.Token);
            if (fresh is null) return;

            // замінюємо у кеші _all
            var idx = _all.FindIndex(p => p.Id == personId);
            if (idx >= 0) _all[idx] = fresh; else _all.Add(fresh);

            // і в відфільтрованій колекції Items (якщо є в ній)
            var shownIdx = Items.Select((p, i) => (p, i)).FirstOrDefault(t => t.p.Id == personId).i;
            if (shownIdx >= 0)
            {
                Items[shownIdx] = fresh;
            }
            else
            {
                // якщо активний фільтр тепер пропускає цю картку — перераховуємо
                ApplyFilter();
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося оновити картку: {ex.Message}");
        }
    }

    // ========= Helpers =========
    private void ApplyFilter()
    {
        IEnumerable<Person> q = _all;

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            q = q.Where(p =>
                (p.FullName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Rnokpp?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Rank?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Callsign?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.PositionUnit?.ShortName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        ResetItems(q);
    }

    private void ResetItems(IEnumerable<Person> people)
    {
        Items.Clear();
        foreach (var p in people) Items.Add(p);
        StateHasChanged();
    }

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