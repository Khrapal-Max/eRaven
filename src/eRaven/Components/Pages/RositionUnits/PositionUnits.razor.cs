//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnits
//-----------------------------------------------------------------------------

using eRaven.Components.Shared.ConfirmModal;
using eRaven.Domain.Entities;
using eRaven.Extensions;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Components.Pages.RositionUnits;

public partial class PositionUnits : IDisposable
{
    // ==== table of position units ====
    protected List<PositionUnit> PositionUnitsList { get; set; } = [];
    protected PositionUnit? Selected { get; set; }

    // ==== ui ====
    private readonly CancellationTokenSource _cts = new();
    protected bool IsLoading { get; private set; } = true;

    // ==== create modal state ====
    private EditContext? _createEditContext;
    protected bool CreateOpen { get; set; }
    protected PositionUnit CreateModel { get; set; } = new();

    // ==== comfirm modal state ====
    private ConfirmModal<PositionUnit> _deactivateModal = default!;

    // ==== dependensy ====
    [Inject] private IPositionUnitRepository PositionUnitRepository { get; set; } = default!;
    [Inject] private IValidator<PositionUnit> PositionUnitValidator { get; set; } = default!;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var positions = await PositionUnitRepository.GetAllPositionUnits(_cts.Token);

            PositionUnitsList = [.. positions.OrderByDescending(x => x.IsActived).ThenBy(x => x.Number)];
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsLoading = false;
        }
    }

    protected void OnAddPositionUnitClick()
    {
        CreateModel = new PositionUnit { Id = Guid.NewGuid(), IsActived = true };
        _createEditContext = new EditContext(CreateModel);
        CreateOpen = true;
    }

    private async Task<bool> CreateAsync()
    {
        if (_createEditContext is null) return false;

        var isValid = await _createEditContext.ValidateWithFluentValidationAsync(PositionUnitValidator, _cts.Token);
        if (!isValid) return false;

        CreateModel.Code = (CreateModel.Code ?? string.Empty).Trim();

        if (await PositionUnitRepository.CodeExistsAsync(CreateModel.Code, _cts.Token))
        {
            _createEditContext.AddFieldError(CreateModel, nameof(PositionUnit.Code), "Посада з таким кодом вже існує");
            return false;
        }

        if (await PositionUnitRepository.ActiveNumberExistsAsync(CreateModel.Number, _cts.Token))
        {
            _createEditContext.AddFieldError(CreateModel, nameof(PositionUnit.Number), "Посада з таким штатним номером уже активна");
            return false;
        }

        try
        {
            await PositionUnitRepository.AddPositionUnit(CreateModel, _cts.Token);
        }
        catch (DbUpdateException)
        {
            _createEditContext.AddFieldError(CreateModel, nameof(PositionUnit.Code), "Посада з таким кодом вже існує");
            return false;
        }

        await LoadAsync();

        // cleanup
        _createEditContext = null;
        CreateModel = new PositionUnit();

        return true;
    }

    protected async Task AskDeactivate(PositionUnit unit)
    {
        var ok = await _deactivateModal.ShowAsync(unit);
        if (!ok) return;

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
