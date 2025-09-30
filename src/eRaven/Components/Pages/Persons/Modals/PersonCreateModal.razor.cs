//-----------------------------------------------------------------------------
// Components/Pages/Persons/Modals/PersonCreateModal.razor.cs
//-----------------------------------------------------------------------------
// PersonCreateModal (code-behind)
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace eRaven.Components.Pages.Persons.Modals;

public partial class PersonCreateModal : ComponentBase
{
    // ========== DI ==========
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private ILogger<PersonCreateModal> Logger { get; set; } = default!;

    // ========== Callback-и ==========
    [Parameter] public EventCallback<bool> OnClose { get; set; }

    // Повертаємо щойно створеного — батько сам оновить список
    [Parameter] public EventCallback<Person> OnCreated { get; set; }

    // ========== Стан ==========
    public bool IsOpen { get; private set; }
    public bool Busy { get; private set; }
    public CreatePersonViewModel Model { get; private set; } = new();

    private FluentValidationValidator? _fv;

    // ---------- API ----------
    public void Open()
    {
        if (Busy) return;
        Model = new CreatePersonViewModel();
        IsOpen = true;
        StateHasChanged();
    }

    private async Task CancelAsync() => await CloseAsync(false);

    private async Task CreateAsync()
    {
        if (Busy) return;

        try
        {
            SetBusy(true);

            // Нормалізація
            var p = new Person
            {
                Id = Guid.NewGuid(),
                LastName = Model.LastName!.Trim(),
                FirstName = Model.FirstName!.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(Model.MiddleName) ? null : Model.MiddleName.Trim(),
                Rnokpp = Model.Rnokpp!.Trim(),
                Rank = Model.Rank!.Trim(),
                BZVP = Model.BZVP!.Trim(),
                Weapon = string.IsNullOrWhiteSpace(Model.Weapon) ? null : Model.Weapon.Trim(),
                Callsign = string.IsNullOrWhiteSpace(Model.Callsign) ? null : Model.Callsign.Trim(),
            };

            var created = await PersonService.CreateAsync(p);

            Toast.ShowSuccess("Картку створено.");
            await OnCreated.InvokeAsync(created);
            await CloseAsync(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FluentValidation.ValidationException ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Toast.ShowError($"Помилка створення: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while creating a person");
            throw;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ---------- Helpers ----------
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
