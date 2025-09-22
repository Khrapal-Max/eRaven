//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// UnassignPositionModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.ViewModels.PositionAssignmentViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PositionAssignments.Modals;

public partial class UnassignPositionModal : ComponentBase
{
    [Parameter] public EventCallback<bool> OnUnassigned { get; set; }
    [Inject] public IPositionAssignmentService PositionAssignmentService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    private bool IsOpen { get; set; }
    private bool _busy;
    private FluentValidationValidator? _validator;

    private Person? _person;
    private PersonPositionAssignment? _active;

    private DateTime _date = DateTime.UtcNow;
    private string _currentTitle = "-";
    private string _openStr = "-";

    private UnassignViewModel Model { get; set; } = new();

    public void Open(Person person, PersonPositionAssignment active)
    {
        _person = person ?? throw new ArgumentNullException(nameof(person));
        _active = active ?? throw new ArgumentNullException(nameof(active));

        _currentTitle = active.PositionUnit.FullName;
        _openStr = active.OpenUtc.ToString("dd.MM.yyyy 'UTC' HH:mm");

        Model = new UnassignViewModel
        {
            PersonId = person.Id,
            Note = string.Empty
        };

        IsOpen = true;
        StateHasChanged();
    }

    private void Close() => IsOpen = false;

    private DateTime BuildUtcOrThrow()
    {
        // закриття фіксуємо на 00:00:00 UTC обраного дня
        return new DateTime(_date.Year, _date.Month, _date.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private async Task OnSubmit()
    {
        if (_person is null) return;

        var closeUtc = BuildUtcOrThrow();

        if (closeUtc < _active!.OpenUtc)
        {
            ToastService.ShowInfo("Дата здання посади повинна бути пізнішою ніж призначення.");
            return;
        };

        try
        {
            _busy = true;

            var ok = await PositionAssignmentService.UnassignAsync(
                _person.Id,
                closeUtc,
                string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note!.Trim(),
                default);

            await OnUnassigned.InvokeAsync(ok);
            if (ok) Close();
        }
        finally { _busy = false; }
    }
}
