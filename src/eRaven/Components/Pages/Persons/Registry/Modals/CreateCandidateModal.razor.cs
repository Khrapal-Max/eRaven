//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModal
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Registry.Modals;

public partial class CreateCandidateModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public EventCallback<CreateCandidateDto> OnCreate { get; set; }

    protected CreateCandidateDto Model { get; set; } = new();

    private EditContext _editContext = default!;
    private bool _busy;

    protected override void OnParametersSet()
    {
        // при першому рендері або кожному відкритті — нова модель/контекст
        if (IsOpen && (_editContext is null || ReferenceEquals(_editContext.Model, Model) == false))
        {
            Model = new CreateCandidateDto();
            _editContext = new EditContext(Model);
        }
    }

    // викликається кнопкою "Створити" з базового Modal
    private async Task<bool> OnCreateAsync()
    {
        if (_busy) return false;

        // Тригеримо валідацію
        var isValid = _editContext.Validate();
        if (!isValid)
            return false; // Modal залишиться відкритим

        _busy = true;
        try
        {
            // trim перед відправкою
            Model.Rnokpp = TrimOrEmpty(Model.Rnokpp);
            Model.LastName = TrimOrEmpty(Model.LastName);
            Model.FirstName = TrimOrEmpty(Model.FirstName);
            Model.MiddleName = TrimOrNull(Model.MiddleName);
            Model.PlannedPosition = TrimOrNull(Model.PlannedPosition);

            await OnCreate.InvokeAsync(Model);

            return true; // Modal закриється
        }
        finally
        {
            _busy = false;
        }
    }

    // опціонально: при Cancel можемо просто скидати форму
    private Task OnCancel()
    {
        // якщо хочеш — скинути модель при cancel:
        Model = new CreateCandidateDto();
        _editContext = new EditContext(Model);

        return Task.CompletedTask;
    }

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}