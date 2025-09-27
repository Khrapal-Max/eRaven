//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionCreateModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Positions.Modals;
public partial class PositionCreateModal : ComponentBase
{
    // ========== DI ==========
    [Inject] private IPositionService PositionService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    // ========== Колбеки ==========
    [Parameter] public EventCallback<bool> OnClose { get; set; }
    [Parameter] public EventCallback<PositionUnitViewModel> OnCreated { get; set; }

    // ========== Стан ==========
    public bool IsOpen { get; private set; }
    public bool Busy { get; private set; }
    public CreatePositionUnitViewModel Model { get; private set; } = new();

    private FluentValidationValidator? _validator;

    public void Open()
    {
        if (Busy) return;
        Model = new CreatePositionUnitViewModel();
        IsOpen = true;
        StateHasChanged();
    }

    // ⚠️ OnValidSubmit вже гарантує, що FluentValidation (включно з async) пройшов.
    private async Task CreateAsync()
    {
        if (Busy) return;

        try
        {
            SetBusy(true);

            // Нормалізація: зручно мати 1 місце, де ми "trim"
            var entity = new PositionUnit
            {
                Code = Model.Code.Trim(),
                ShortName = Model.ShortName.Trim(),
                SpecialNumber = Model.SpecialNumber.Trim(),
                OrgPath = Model.OrgPath.Trim(),
                IsActived = true
            };

            var created = await PositionService.CreatePositionAsync(entity);

            var vm = new PositionUnitViewModel
            {
                Id = created.Id,
                Code = created.Code ?? string.Empty,
                ShortName = created.ShortName,
                SpecialNumber = created.SpecialNumber,
                FullName = created.FullName,
                CurrentPersonFullName = created.CurrentPerson?.FullName,
                IsActived = created.IsActived
            };

            Toast.ShowSuccess("Посаду створено.");
            await OnCreated.InvokeAsync(vm);
            await CloseAsync(true);
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CancelAsync() => await CloseAsync(false);

    private async Task CloseAsync(bool result)
    {
        if (!IsOpen) return;
        IsOpen = false;
        await OnClose.InvokeAsync(result);
        await InvokeAsync(StateHasChanged);
    }

    private void SetBusy(bool value)
    {
        Busy = value;
        StateHasChanged();
    }
}