//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard (code-behind)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons;

public partial class PersonCard : ComponentBase, IDisposable
{
    // ===================== Route / DI =====================
    [Parameter] public Guid Id { get; set; }

    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IPersonStatusService PersonStatusService { get; set; } = default!; // ⬅️ ДОДАЛИ
    [Inject] private IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // ===================== UI state =====================
    protected bool _initialLoading = true;
    protected Person? _person;
    protected bool _historyOpen;
    protected bool _assignHistoryOpen;


    // Активний статус (із приміткою)
    protected PersonStatus? _activeStatus; // ⬅️ ДОДАЛИ

    // Редагування
    protected bool _editMode;
    protected bool _editBusy;
    protected EditPersonViewModel _editModel = new();

    // Історія (поки лічильник)
    protected int _assignHistoryCount = 0;
    protected string AssignHistoryCountText => _assignHistoryCount == 0 ? "0" : _assignHistoryCount.ToString();

    protected int _historyCount = 0;
    protected string HistoryCountText => _historyCount == 0 ? "0" : _historyCount.ToString();

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        _initialLoading = false;
    }

    protected void GoBack() => Nav.NavigateTo("/persons");

    protected void BeginEdit()
    {
        if (_person is null) return;
        _editModel = EditPersonViewModel.From(_person);
        _editMode = true;
    }

    protected void CancelEdit()
    {
        _editMode = false;
        _editBusy = false;
    }

    protected async Task SaveEditAsync()
    {
        if (_person is null || _editBusy) return;

        _editBusy = true;
        try
        {
            var updated = MapToUpdatablePerson(_person.Id, _editModel, _person.PositionUnitId);
            await PersonService.UpdateAsync(updated);
            await ReloadAsync(); // оновлюємо картку з серверу

            Toast.ShowSuccess("Зміни збережено.");
            _editMode = false;
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося зберегти. " + ex.Message);
        }
        finally
        {
            _editBusy = false;
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            _person = await PersonService.GetByIdAsync(Id);

            if (_person is null)
            {
                Toast.ShowWarning("Особа не знайдена.");
                return;
            }

            _activeStatus = await PersonStatusService.GetActiveAsync(_person.Id);
            _historyCount = (await PersonStatusService.GetHistoryAsync(_person.Id))
                            .Count(s => s.IsActive);

            // ⬇️ нове: лічильник історії посад
            _assignHistoryCount = (await PositionAssignmentService.GetHistoryAsync(_person.Id, limit: 500)).Count;
        }
        catch (Exception ex)
        {
            Toast.ShowError("Помилка завантаження картки. " + ex.Message);
        }
    }

    private static Person MapToUpdatablePerson(Guid id, EditPersonViewModel vm, Guid? keepPositionUnitId) => new()
    {
        Id = id,
        LastName = vm.LastName!.Trim(),
        FirstName = vm.FirstName!.Trim(),
        MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName.Trim(),
        Rnokpp = vm.Rnokpp.Trim(),
        Rank = vm.Rank.Trim(),
        Callsign = string.IsNullOrWhiteSpace(vm.Callsign) ? null : vm.Callsign.Trim(),
        BZVP = vm.BZVP.Trim(),
        Weapon = string.IsNullOrWhiteSpace(vm.Weapon) ? null : vm.Weapon.Trim(),
        PositionUnitId = keepPositionUnitId
    };

    protected Task OpenHistory()
    {
        _historyOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleHistoryClose()
    {
        _historyOpen = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected Task OpenAssignHistory()
    {
        _assignHistoryOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleAssignHistoryClose()
    {
        _assignHistoryOpen = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
