//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// UnassignPositionModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.ViewModels.PositionAssignmentViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments.Modals;

public partial class UnassignPositionModal : ComponentBase
{
    [Parameter] public EventCallback<bool> OnUnassigned { get; set; }
    [Inject] public IPositionAssignmentService AssignmentService { get; set; } = default!;

    private bool IsOpen { get; set; }
    private bool _busy;

    private FluentValidationValidator? _validator;

    private Person? _person;
    private PersonPositionAssignment? _active;

    private string _date = "";
    private string _hour = "00";
    private string _min = "00";

    private string _currentTitle = "-";
    private string _openStr = "-";

    private UnassignViewModel Model { get; set; } = new();

    public void Open(Person person, PersonPositionAssignment active)
    {
        _person = person;
        _active = active;

        var now = DateTime.UtcNow;
        _date = now.ToString("yyyy-MM-dd");
        _hour = now.Hour.ToString("00");
        _min = now.Minute >= 30 ? "30" : "00";

        _currentTitle = active.PositionUnit.FullName;
        _openStr = active.OpenUtc.ToString("dd.MM.yyyy HH:mm 'UTC'");

        Model = new UnassignViewModel { PersonId = person.Id, Note = string.Empty };

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

            var ok = await AssignmentService.UnassignAsync(
                _person.Id,
                BuildUtc(),
                Model.Note,
                default);

            await OnUnassigned.InvokeAsync(ok);
            if (ok) Close();
        }
        finally { _busy = false; }
    }
}
