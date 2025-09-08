//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons;

public partial class PersonCard : ComponentBase, IDisposable
{
    // =============== Route/DI ===============
    [Parameter] public Guid Id { get; set; }

    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // =============== UI state ===============
    protected bool _initialLoading = true;
    protected Person? _person;

    protected bool _editMode;
    protected bool _editBusy;
    protected EditModel _editModel = new();

    protected int _historyCount = 0; // поки просто число
    protected string HistoryCountText => _historyCount == 0 ? "0" : _historyCount.ToString();

    // =============== Lifecycle ===============
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _person = await PersonService.GetByIdAsync(Id);
            if (_person is null) Toast.ShowWarning("Особа не знайдена.");
        }
        catch (Exception ex)
        {
            Toast.ShowError("Помилка завантаження картки. " + ex.Message);
        }
        finally { _initialLoading = false; }
    }

    // =============== Actions ===============
    protected void GoBack() => Nav.NavigateTo("/persons");

    protected void BeginEdit()
    {
        if (_person is null) return;
        _editModel = EditModel.From(_person);
        _editMode = true;
    }

    protected void CancelEdit()
    {
        _editMode = false;
        _editBusy = false;
    }

    protected async Task SaveEditAsync()
    {
        if (_person is null) return;

        try
        {
            _editBusy = true;

            var updated = new Person
            {
                Id = _person.Id,
                LastName = _editModel.LastName!.Trim(),
                FirstName = _editModel.FirstName!.Trim(),
                MiddleName = NullIfWhite(_editModel.MiddleName),
                Rnokpp = _editModel.Rnokpp?.Trim() ?? string.Empty,
                Rank = NullIfWhite(_editModel.Rank),
                Callsign = NullIfWhite(_editModel.Callsign),
                BZVP = NullIfWhite(_editModel.BZVP),
                Weapon = NullIfWhite(_editModel.Weapon),
                // не чіпаємо посаду/статус тут
                PositionUnitId = _person.PositionUnitId,
                StatusKindId = _person.StatusKindId
            };

            var ok = await PersonService.UpdateAsync(updated);
            if (!ok) Toast.ShowWarning("Зміни не збережено.");
            else
            {
                _person = await PersonService.GetByIdAsync(Id);
                Toast.ShowSuccess("Зміни збережено.");
                _editMode = false;
            }
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося зберегти. " + ex.Message);
        }
        finally { _editBusy = false; }
    }

    protected Task OpenHistory()
    {
        Toast.ShowInfo("На реалізації");
        return Task.CompletedTask;
    }

    private static string? NullIfWhite(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // =============== Edit VM ===============
    protected class EditModel
    {
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? Rnokpp { get; set; }
        public string? Rank { get; set; }
        public string? Callsign { get; set; }
        public string? BZVP { get; set; }
        public string? Weapon { get; set; }

        public static EditModel From(Person p) => new()
        {
            LastName = p.LastName,
            FirstName = p.FirstName,
            MiddleName = p.MiddleName,
            Rnokpp = p.Rnokpp,
            Rank = p.Rank,
            Callsign = p.Callsign,
            BZVP = p.BZVP,
            Weapon = p.Weapon
        };
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
