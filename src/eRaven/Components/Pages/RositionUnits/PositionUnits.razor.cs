//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnits
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.RositionUnits;

public partial class PositionUnits : IDisposable
{
    protected List<PositionUnit> PositionUnitsList { get; set; } = [];
    protected PositionUnit? Selected { get; set; }

    protected bool IsLoading { get; private set; } = true;

    private readonly CancellationTokenSource _cts = new();

    [Inject] private IPositionUnitRepository PositionUnitRepository { get; set; } = default!;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            PositionUnitsList = [.. await PositionUnitRepository.GetAllPositionUnits(_cts.Token)];
        }
        catch (OperationCanceledException) { }
        finally 
        { 
            IsLoading = false; 
        }
    }

    protected void OnRowClick(PositionUnit unit) => Selected = unit;
    protected void CreateNew() { /* TODO */ }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}