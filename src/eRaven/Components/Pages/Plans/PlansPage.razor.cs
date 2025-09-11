// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlansPage (code-behind)
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlansPage : ComponentBase, IDisposable
{
    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();

    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    private List<Plan> _all = [];
    private IReadOnlyList<Plan> _view = [];

    // -------------------- Lifecycle --------------------

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            var items = await PlanService.GetAllPlansAsync(_cts.Token);
            _all = [.. items];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Не вдалося завантажити плани: {ex.Message}");
            _all.Clear();
            _view = [];
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -------------------- Search --------------------

    protected Task OnSearchAsync()
    {
        ApplyFilter();
        return Task.CompletedTask;
    }

    private void ApplyFilter()
    {
        var s = (Search ?? string.Empty).Trim();

        IEnumerable<Plan> query = _all;

        if (s.Length > 0)
        {
            bool Has(string? src) => !string.IsNullOrWhiteSpace(src) &&
                                     src.Contains(s, StringComparison.OrdinalIgnoreCase);

            query = query.Where(p => Has(p.PlanNumber));
        }

        _view = query
            .OrderByDescending(p => p.RecordedUtc)
            .ToList()
            .AsReadOnly();

        StateHasChanged();
    }

    // -------------------- Navigation --------------------

    private void GoCreate() => Nav.NavigateTo("/plans/create");

    private static string PlanDetailsHref(Guid id) => $"/plans/{id:D}";

    // -------------------- Utils --------------------

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
