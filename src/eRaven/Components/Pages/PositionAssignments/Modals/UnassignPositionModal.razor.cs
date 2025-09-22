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

    private string _date = string.Empty;              // yyyy-MM-dd
    private string _currentTitle = "-";
    private string _openStr = "-";

    private UnassignViewModel Model { get; set; } = new();

    public void Open(Person person, PersonPositionAssignment active)
    {
        _person = person ?? throw new ArgumentNullException(nameof(person));
        _active = active ?? throw new ArgumentNullException(nameof(active));

        // сьогоднішня дата за UTC (00:00:00)
        _date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

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

    private void OnDateChanged(ChangeEventArgs e)
        => _date = Convert.ToString(e.Value) ?? string.Empty;

    private DateTime BuildUtcOrThrow()
    {
        if (string.IsNullOrWhiteSpace(_date) || !DateOnly.TryParse(_date, out var d))
            throw new InvalidOperationException("Оберіть коректну дату.");

        // закриття фіксуємо на 00:00:00 UTC обраного дня
        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private async Task OnSubmit()
    {
        if (_person is null) return;

        try
        {
            _busy = true;

            var closeUtc = BuildUtcOrThrow();

            var ok = await AssignmentService.UnassignAsync(
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
