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
    private string _date = string.Empty; // yyyy-MM-dd

    private AssignViewModel Model { get; set; } = new();

    public void Open(Person person)
    {
        _person = person ?? throw new ArgumentNullException(nameof(person));

        var todayUtc = DateTime.UtcNow.Date; // опівніч UTC
        _date = todayUtc.ToString("yyyy-MM-dd");

        Model = new AssignViewModel
        {
            PersonId = person.Id,
            PositionUnitId = Guid.Empty,
            Note = null
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

        // Призначення фіксуємо на 00:00:00 UTC обраного дня
        return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private async Task OnSubmit()
    {
        if (_person is null) return;

        try
        {
            _busy = true;

            var openUtc = BuildUtcOrThrow();

            var created = await AssignmentService.AssignAsync(
                _person.Id,
                Model.PositionUnitId,
                openUtc,
                string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note!.Trim(),
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

    private static string Trunc(string? s, int max)
       => string.IsNullOrEmpty(s) || s.Length <= max ? (s ?? string.Empty)
                                                     : string.Concat(s.AsSpan(0, max - 1), "…");
}
