//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class ExcludeDrawer
{
    // =========================
    // Parameters
    // =========================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public Guid PersonId { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<ExcludeDto> OnSubmit { get; set; }

    // =========================
    // DI
    // =========================

    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> DetailsQuery { get; set; } = default!;

    // =========================
    // State
    // =========================

    private bool _busy;
    private bool _wasOpen;
    private bool _loading;

    private PersonDetailsDto? _person;

    private EditContext _editContext = default!;
    protected ExcludeDto Model { get; set; } = new();

    private IReadOnlyDictionary<string, object>? SubmitAttrs =>
        (_busy || _loading || _person is null)
            ? new Dictionary<string, object> { ["disabled"] = "disabled" }
            : null;

    // =========================
    // Lifecycle
    // =========================

    protected override void OnInitialized()
    {
        Model = new ExcludeDto();
        _editContext = new EditContext(Model);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await LoadAsync();
            return;
        }

        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }
    }

    // =========================
    // Data
    // =========================

    private async Task LoadAsync()
    {
        Reset();

        if (PersonId == Guid.Empty)
        {
            _person = null;
            return;
        }

        _loading = true;
        try
        {
            _person = await DetailsQuery.HandleAsync(new GetPersonDetailsQuery(PersonId));
            if (_person is not null)
            {
                Model.Id = _person.Id;

                // дефолт: сьогодні; можна теж поставити сьогодні локально
                Model.EffectiveDate = DateOnly.FromDateTime(DateTime.Now);

                // пусто — обовʼязкове валідацією
                Model.Reason = string.Empty;
            }

            _editContext = new EditContext(Model);
        }
        finally
        {
            _loading = false;
        }
    }

    private void Reset()
    {
        _busy = false;
        _loading = false;

        _person = null;
        Model = new ExcludeDto();
        _editContext = new EditContext(Model);
    }

    // =========================
    // Actions
    // =========================

    private async Task SubmitAsync()
    {
        if (_busy || _loading || _person is null)
            return;

        _busy = true;
        try
        {
            // normalize
            Model.Reason = TrimOrEmpty(Model.Reason);

            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(new ExcludeDto
                {
                    Id = Model.Id,
                    EffectiveDate = Model.EffectiveDate,
                    Reason = Model.Reason
                });
            }

            await IsOpenChanged.InvokeAsync(false);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }

    // =========================
    // Helpers
    // =========================

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
}
