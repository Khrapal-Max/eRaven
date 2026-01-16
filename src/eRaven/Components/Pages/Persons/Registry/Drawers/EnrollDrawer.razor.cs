//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Registry.Drawers;

public partial class EnrollDrawer
{
    // =========================
    // Parameters
    // =========================
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public Guid PersonId { get; set; }
    [Parameter] public EventCallback<EnrollDto> OnSubmit { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    // =========================
    // DI
    // =========================
    [Inject] public IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?> DetailsQuery { get; set; } = default!;
    [Inject] public IRankCatalog RankCatalog { get; set; } = default!;

    // =========================
    // UI state
    // =========================
    private bool _busy;
    private bool _wasOpen;
    private bool _loading;

    private PersonDetailsDto? _person;
    private IReadOnlyList<RankOption> _ranks = [];

    private EditContext _editContext = default!;
    protected EnrollDto Model { get; set; } = new();

    // коли юзер вводив PositionSort у Unit, а потім перемикав Kind
    private int? _unitPositionSortBackup;

    private static readonly EnrollmentKind[] _kinds = Enum.GetValues<EnrollmentKind>();
    private bool IsUnitKind => Model.Kind == EnrollmentKind.Unit;

    private IReadOnlyDictionary<string, object>? SubmitAttrs =>
        (_busy || _loading || _person is null)
            ? new Dictionary<string, object> { ["disabled"] = "disabled" }
            : null;

    // =========================
    // Lifecycle
    // =========================
    protected override void OnInitialized() => Reset();

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
    // Load / Reset
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
            _ranks = RankCatalog.GetActive();

            _person = await DetailsQuery.HandleAsync(new GetPersonDetailsQuery(PersonId));
            if (_person is null)
                return;

            Model.Id = _person.Id;
            Model.Kind = EnrollmentKind.Unit;
            Model.Reference = _person.EnrollmentReference;
            Model.EnrollDate = DateOnly.FromDateTime(DateTime.Now);

            Model.Rank = _person.Rank ?? string.Empty;
            Model.PositionSort = _person.PositionSort ?? 0;
            Model.Position = _person.Position ?? string.Empty;

            _unitPositionSortBackup = Model.PositionSort;

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
        _unitPositionSortBackup = null;

        _editContext = new EditContext(Model);
    }

    // =========================
    // Actions
    // =========================
    private void OnKindAfterChanged()
    {
        if (Model.Kind != EnrollmentKind.Unit)
        {
            if (Model.PositionSort is > 0 and not 9999)
                _unitPositionSortBackup = Model.PositionSort;

            Model.PositionSort = 9999;
        }
        else
        {
            var restored =
                _unitPositionSortBackup
                ?? _person?.PositionSort
                ?? 1;

            if (restored == 9999) restored = 1;
            Model.PositionSort = restored;
        }

        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Kind)));
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.PositionSort)));
    }

    private async Task SubmitAsync()
    {
        if (_busy || _loading || _person is null)
            return;

        _busy = true;
        try
        {
            Model.Reference = TrimOrNull(Model.Reference);
            Model.Reason = TrimOrEmpty(Model.Reason);
            Model.Rank = TrimOrEmpty(Model.Rank);
            Model.Position = TrimOrEmpty(Model.Position);

            if (Model.Kind != EnrollmentKind.Unit)
                Model.PositionSort = 9999;

            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync(new EnrollDto
                {
                    Id = Model.Id,
                    Kind = Model.Kind,
                    Reference = Model.Reference,
                    Reason = Model.Reason,
                    EnrollDate = Model.EnrollDate,
                    Rank = Model.Rank,
                    PositionSort = Model.PositionSort,
                    Position = Model.Position
                });
            }

            await IsOpenChanged.InvokeAsync(false);
        }
        finally
        {
            _busy = false;
        }
    }

    private void SelectRank(string rank)
    {
        Model.Rank = rank;
        _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.Rank)));
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
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string KindTitle(EnrollmentKind k) => k switch
    {
        EnrollmentKind.Unit => "У штат (Unit)",
        EnrollmentKind.AttachedByOrder => "Приряджений БР",
        EnrollmentKind.AttachedByList => "Приряджений наказом",
        _ => k.ToString()
    };
}