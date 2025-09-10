//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegisterStatuses
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Registers;

public partial class RegisterStatuses : ComponentBase, IDisposable
{   
    private bool Busy { get; set; }
    private string? Search { get; set; }

    private IReadOnlyList<PersonStatus> _statuses = [];

    private readonly CancellationTokenSource _cts = new();

    [Inject] protected IPersonStatusService PersonStatusService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadStatusesAsync();
    }

    private async Task ReloadStatusesAsync()
    {
        var statuses = await PersonStatusService.GetAllAsync(ct: _cts.Token);

        _statuses = statuses.ToList().AsReadOnly();

        await InvokeAsync(StateHasChanged);
    }

    protected Task OnSearchAsync()
    {
        ApplyFilterAndSort();
        return Task.CompletedTask;
    }

    private void ApplyFilterAndSort()
    {

    }

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }

    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
