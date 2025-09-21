//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignPositionModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments.Modals;

public partial class AssignPositionModal : ComponentBase
{
    [Parameter] public IReadOnlyList<PositionUnit> Positions { get; set; } = [];
    [Parameter] public EventCallback<PersonPositionAssignment> OnAssigned { get; set; }

    [Inject] public IPositionAssignmentService AssignmentService { get; set; } = default!;

    [Inject] public IToastService ToastService { get; set; } = default!;

    private bool IsOpen { get; set; }
    private bool _busy;

    private FluentValidationValidator? _validator;

    private Person? _person;

    private string _date = "";
    private string _hour = "00";
    private string _min = "00";

    private AssignViewModel Model { get; set; } = new();

    public void Open(Person person)
    {
        _person = person ?? throw new ArgumentNullException(nameof(person));

        var now = DateTime.UtcNow;
        _date = now.ToString("yyyy-MM-dd");
        _hour = now.Hour.ToString("00");
        _min = now.Minute >= 30 ? "30" : "00";

        Model = new AssignViewModel { PersonId = person.Id };

        IsOpen = true;
        StateHasChanged();
    }

    private void Close() => IsOpen = false;

    private void OnDateChanged(ChangeEventArgs e) => _date = Convert.ToString(e.Value) ?? "";
    private void OnHourChanged(ChangeEventArgs e) => _hour = Convert.ToString(e.Value) ?? "00";
    private void OnMinChanged(ChangeEventArgs e) => _min = Convert.ToString(e.Value) ?? "00";

    private DateTime BuildUtc()
    {
        var d = DateOnly.Parse(_date);
        var h = int.Parse(_hour);
        var m = int.Parse(_min);
        return new DateTime(d.Year, d.Month, d.Day, h, m, 0, DateTimeKind.Utc);
    }

    private async Task OnSubmit()
    {
        if (_person is null) return;

        try
        {
            _busy = true;

            var openUtc = BuildUtc();

            var created = await AssignmentService.AssignAsync(
                _person.Id,
                Model.PositionUnitId,
                openUtc,
                Model.Note,
                default);

            await OnAssigned.InvokeAsync(created);
            Close();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally { _busy = false; }
    }
}
