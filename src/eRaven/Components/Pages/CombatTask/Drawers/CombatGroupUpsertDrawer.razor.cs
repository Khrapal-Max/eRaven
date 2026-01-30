//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupUpsertDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

public partial class CombatGroupUpsertDrawer
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    // "create" | "edit"
    [Parameter] public string Mode { get; set; } = "create";

    [Parameter] public Guid GroupId { get; set; }
    [Parameter] public CreateCombatGroupModel? Initial { get; set; }

    [Parameter] public EventCallback<CreateCombatGroupModel> OnCreated { get; set; }
    [Parameter] public EventCallback<UpdateCombatGroupModel> OnUpdated { get; set; }

    // TODO: під’єднай свої Query handlers:
    // - GetOpenMissionsQuery => List<MissionOption>
    // - SearchPersonsQuery(search) => List<PersonOption>
    // Поки дам прості заглушки для типів і місць виклику.

    private sealed record MissionOption(Guid Id, string Display);
    private sealed record PersonOption(Guid Id, string Display);

    private string _sourceDocNo = string.Empty;
    private ActionKind _action = ActionKind.Start;
    private DateOnly _actionDate = DateOnly.FromDateTime(DateTime.Now);

    private Guid _missionId = Guid.Empty;
    private string _missionDisplay = string.Empty;

    private string _search = string.Empty;
    private List<MissionOption> _missions = [];
    private List<PersonOption> _persons = [];
    private HashSet<Guid> _selected = [];

    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        _error = null;

        // 1) load missions (TODO)
        // _missions = await MissionsQuery(...);
        // Заглушка:
        if (_missions.Count == 0)
        {
            _missions =
            [
                new MissionOption(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Mission #1"),
                new MissionOption(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mission #2"),
            ];
        }

        // 2) prefill for edit
        if (Mode == "edit" && Initial is not null)
        {
            _sourceDocNo = Initial.SourceDocNo;
            _action = Initial.Action;
            _actionDate = Initial.ActionDate;
            _missionId = Initial.MissionId;
            _missionDisplay = Initial.MissionDisplaySnapshot;
        }
        else if (Mode == "create")
        {
            _sourceDocNo = "";
            _action = ActionKind.Start;
            _actionDate = DateOnly.FromDateTime(DateTime.Now);
            _missionId = Guid.Empty;
            _missionDisplay = "";
            _selected = [];
        }

        await LoadPersonsAsync();
    }

    private async Task LoadPersonsAsync()
    {
        if (Mode != "create") return;

        // TODO: persons search query by _search
        // _persons = await PersonsQuery(_search);

        // Заглушка:
        _persons =
        [
            new PersonOption(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Іваненко Іван — стрілець"),
            new PersonOption(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Петренко Петро — водій"),
            new PersonOption(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Сидоренко Сидір — оператор"),
        ];

        if (!string.IsNullOrWhiteSpace(_search))
            _persons = [.. _persons.Where(p => p.Display.Contains(_search, StringComparison.OrdinalIgnoreCase))];
    }

    private void TogglePerson(Guid id, bool value)
    {
        if (value) _selected.Add(id);
        else _selected.Remove(id);
    }

    private bool CanSave
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_sourceDocNo)) return false;
            if (_missionId == Guid.Empty) return false;

            if (Mode == "create" && _selected.Count == 0) return false;

            return true;
        }
    }

    private async Task Save()
    {
        _error = null;

        var missionDisplay = _missions.FirstOrDefault(x => x.Id == _missionId)?.Display ?? _missionDisplay;
        if (string.IsNullOrWhiteSpace(missionDisplay))
            missionDisplay = "—";

        if (Mode == "create")
        {
            var model = new CreateCombatGroupModel(
                SourceDocNo: _sourceDocNo.Trim(),
                Action: _action,
                MissionId: _missionId,
                MissionDisplaySnapshot: missionDisplay,
                ActionDate: _actionDate,
                PersonIds: [.. _selected]);

            await OnCreated.InvokeAsync(model);
            await Close();
            return;
        }

        // edit
        var upd = new UpdateCombatGroupModel(
            GroupId: GroupId,
            SourceDocNo: _sourceDocNo.Trim(),
            Action: _action,
            MissionId: _missionId,
            MissionDisplaySnapshot: missionDisplay,
            ActionDate: _actionDate);

        await OnUpdated.InvokeAsync(upd);
        await Close();
    }

    private async Task Close()
    {
        await IsOpenChanged.InvokeAsync(false);
    }
}
