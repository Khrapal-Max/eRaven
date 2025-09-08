//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;

namespace eRaven.Components.Pages.Persons;

public partial class PersonsPage : ComponentBase, IDisposable
{
    // =============== DI ===============
    [Inject] protected IPersonService PersonService { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;

    // =============== UI state ===============
    protected bool Busy { get; private set; }
    protected string? Search { get; set; }
    protected Person? Selected { get; set; }

    private readonly CancellationTokenSource _cts = new();

    // Джерело даних
    private List<Person> _all = [];
    protected ObservableCollection<Person> Items { get; private set; } = [];

    // =============== Lifecycle ===============
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    // =============== Handlers ===============
    protected async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);
            _all = [.. await PersonService.SearchAsync(null, _cts.Token)];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося завантажити картки: {ex.Message}");
        }
        finally { SetBusy(false); }
    }

    protected Task OnSearchAsync()
    {
        ApplyFilter();
        return Task.CompletedTask;
    }

    protected void OnRowClick(Person p) => Selected = p;

    protected Task CreateAsync()
    {
        Toast.ShowInfo("На реалізації"); // відкриємо модал пізніше
        return Task.CompletedTask;
    }

    protected Task ExportAsync()
    {
        Toast.ShowInfo("На реалізації");
        return Task.CompletedTask;
    }

    protected Task ImportAsync()
    {
        Toast.ShowInfo("На реалізації");
        return Task.CompletedTask;
    }

    // =============== Helpers ===============
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

    private void ResetItems(IEnumerable<Person> items)
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

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
