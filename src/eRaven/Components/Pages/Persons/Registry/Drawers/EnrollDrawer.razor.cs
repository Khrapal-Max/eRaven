//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class EnrollDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public Guid PersonId { get; set; }

    // батько виконує операцію
    [Parameter] public EventCallback<EnrollDto> OnSubmit { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> DetailsQuery { get; set; } = default!;
    [Inject] public IRankCatalog RankCatalog { get; set; } = default!;

    private IReadOnlyDictionary<string, object>? SubmitAttrs =>
        (_busy || _loading || _person is null)
            ? new Dictionary<string, object> { ["disabled"] = "disabled" }
            : null;
    private IReadOnlyList<RankOption> _ranks = [];

    private bool _busy;
    private bool _wasOpen;
    private bool _loading;

    private PersonDetailsDto? _person;

    private EditContext _editContext = default!;
    protected EnrollDto Model { get; set; } = new();

    private static readonly EnrollmentKind[] _kinds = Enum.GetValues<EnrollmentKind>();

    protected override void OnInitialized()
    {
        Model = new EnrollDto();
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
            _ranks = RankCatalog.GetActive();

            _person = await DetailsQuery.HandleAsync(new GetPersonDetailsQuery(PersonId));

            if (_person is not null)
            {
                Model.Id = _person.Id;

                Model.Kind = EnrollmentKind.Unit;
                Model.Reference = _person.EnrollmentReference;
                Model.EnrollDate = DateOnly.FromDateTime(DateTime.UtcNow);

                // ✅ дефолт для селекта — якщо було звання, воно стане вибраним
                Model.Rank = _person.Rank ?? string.Empty;

                Model.Position = _person.Position ?? string.Empty;
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
        _ranks = [];
        Model = new EnrollDto();
        _editContext = new EditContext(Model);
    }

    private async Task SubmitAsync()
    {
        if (_busy || _loading || _person is null)
            return;

        _busy = true;
        try
        {
            // trim/normalize
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Reason = TrimOrEmpty(Model.Reason);

            Model.Rank = TrimOrEmpty(Model.Rank);
            Model.Position = TrimOrEmpty(Model.Position);

            if (OnSubmit.HasDelegate)
            {
                var dto = new EnrollDto
                {
                    Id = Model.Id,
                    Kind = Model.Kind,
                    Reference = Model.Reference,
                    Reason = Model.Reason,
                    EnrollDate = Model.EnrollDate,
                    Rank = Model.Rank,
                    Position = Model.Position
                };

                await OnSubmit.InvokeAsync(dto);
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

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string KindTitle(EnrollmentKind k) => k switch
    {
        EnrollmentKind.Unit => "У штат (Unit)",
        EnrollmentKind.AttachedByOrder => "Приряджений БР",
        EnrollmentKind.AttachedByList => "Приряджений наказом",
        _ => k.ToString()
    };
}