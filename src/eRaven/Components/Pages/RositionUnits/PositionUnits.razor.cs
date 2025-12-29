//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnits
//-----------------------------------------------------------------------------

using eRaven.Components.Shared.ConfirmModal;
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
    private ConfirmModal<PositionUnit> _deactivateModal = default!;

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
    protected void OnAddPositionUnitClick()
    {
        /* TODO */ 
    }

    protected async Task AskDeactivate(PositionUnit unit)
    {
        var ok = await _deactivateModal.ShowAsync(unit);
        if (!ok) return;

        //* TODO Перевірка чи можна деактивувати посаду, можливо на ній стоїть людина

        await PositionUnitRepository.DeActivatedPositionUnit(unit.Id, _cts.Token);
        await LoadAsync();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}