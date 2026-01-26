//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDocumentLineEditor
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Components.Pages.CombatTask;

public partial class PlanningDocumentLineEditor
{
    [Parameter] public Guid DocumentId { get; set; }
    [Parameter] public Guid? LineId { get; set; }
    private bool IsEdit => LineId is not null;

    [Inject] public NavigationManager Nav { get; set; } = default!;

    // TODO: підстав свої контракти
    // [Inject] public IQueryHandler<SearchPersonsForPlanningQuery, IReadOnlyList<PersonPickRowDto>> PersonsQuery { get; set; } = default!;
    // [Inject] public ICommandHandler<UpsertCombatTaskPlanLineCommand> SaveLine { get; set; } = default!;

    private bool _loading;
    private bool _busy;
    private string? _error;

    private EditContext _editContext = default!;
    protected LineEditorModel Model { get; set; } = new();

    private string? _personSearch;
    private List<PersonPickRowDto>? _personResults;
    private PersonPickRowDto? _selectedPerson;

    protected override void OnInitialized()
    {
        Reset();
    }

    protected override async Task OnParametersSetAsync()
        => await LoadAsync();

    private void Reset()
    {
        _error = null;
        _busy = false;

        Model = new LineEditorModel
        {
            Kind = CombatTaskPlanLineKind.Start,
            ActionDate = DateOnly.FromDateTime(DateTime.Today),
            Mode = CombatTaskMode.Day,
            IsActual = true
        };

        _editContext = new EditContext(Model);
        _personSearch = null;
        _personResults = null;
        _selectedPerson = null;
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            // TODO:
            // 1) перевірити що документ існує і Draft
            // 2) якщо IsEdit => Load line by id, заповнити Model + _selectedPerson(snapshot)
            // Поки просто Reset.
            if (!IsEdit)
                Reset();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnPersonSearchInput(ChangeEventArgs e)
    {
        _personSearch = Convert.ToString(e.Value);

        var q = (_personSearch ?? "").Trim();
        if (q.Length < 2)
        {
            _personResults = null;
            return;
        }

        try
        {
            // TODO: PersonsQuery
            // var res = await PersonsQuery.HandleAsync(new SearchPersonsForPlanningQuery(q));
            // _personResults = res.ToList();

            _personResults = []; // заглушка
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _personResults = [];
        }
    }

    private void SelectPerson(PersonPickRowDto p)
    {
        _selectedPerson = p;
        _personResults = null;

        Model.PersonId = p.PersonId;
        Model.RNOKPP = p.RNOKPP;
        Model.FullName = p.FullName;
        Model.Rank = p.Rank;
        Model.Position = p.Position;
        Model.Weapon = p.Weapon;
        Model.Callsign = p.Callsign;
    }

    private bool DisabledSave =>
        _busy
        || Model.PersonId == Guid.Empty
        || string.IsNullOrWhiteSpace(Model.PositionalArea)
        || string.IsNullOrWhiteSpace(Model.GroupName)
        || string.IsNullOrWhiteSpace(Model.Goal);

    private async Task SubmitAsync()
    {
        if (_busy) return;

        _busy = true;
        _error = null;

        try
        {
            // TODO: зібрати CombatTaskPlanLineInputDto або команду upsert
            // - для End: AssignmentIdRaw -> Guid? (якщо ввели)
            // - LineId: якщо IsEdit => існуючий, інакше новий Guid
            //
            // await SaveLine.HandleAsync(new UpsertCombatTaskPlanLineCommand(...));

            Nav.NavigateTo($"/planning-documents/{DocumentId}");
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    // Мінімальний DTO для picker (щоб не тягнути PersonDto)
    private sealed record PersonPickRowDto(
        Guid PersonId,
        string FullName,
        string RNOKPP,
        string? Rank,
        string? Position,
        string? Weapon,
        string? Callsign
    );

    // Model для форми (DataAnnotations можна додати пізніше)
    public sealed class LineEditorModel
    {
        [Required] public CombatTaskPlanLineKind Kind { get; set; }
        [Required] public Guid PersonId { get; set; }

        [Required] public DateOnly ActionDate { get; set; }

        // snapshot (заповнюється після вибору людини)
        public string RNOKPP { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Rank { get; set; }
        public string? Position { get; set; }
        public string? Weapon { get; set; }
        public string? Callsign { get; set; }

        [Required, MinLength(1)] public string PositionalArea { get; set; } = "";
        [Required, MinLength(1)] public string GroupName { get; set; } = "";
        public string? AssetType { get; set; }

        [Required] public CombatTaskMode Mode { get; set; }
        [Required, MinLength(1)] public string Goal { get; set; } = "";

        public bool IsActual { get; set; } = true;
        public string? Note { get; set; }

        // End: optional
        public string? AssignmentIdRaw { get; set; }
    }
}