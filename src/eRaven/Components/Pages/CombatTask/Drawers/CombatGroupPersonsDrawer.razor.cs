//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatGroupPersonsDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask.Drawers;

public partial class CombatGroupPersonsDrawer
{
    /*[Inject] public IQueryHandler<>*/

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public Guid GroupId { get; set; }
    [Parameter] public IReadOnlyList<Guid> InitialPersonIds { get; set; } = [];

    [Parameter] public EventCallback<ReplaceGroupPersonsModel> OnSaved { get; set; }

    private sealed record PersonOption(Guid Id, string Display);

    private string _search = string.Empty;
    private List<PersonOption> _persons = [];
    private HashSet<Guid> _selected = [];

    protected override async Task OnParametersSetAsync()
    {
        if (!IsOpen) return;

        _selected = [.. InitialPersonIds.Where(x => x != Guid.Empty)];

        await LoadPersonsAsync();
    }

    private async Task LoadPersonsAsync()
    {
        // TODO: реальний пошук по PersonRead через Query handler.
        // Заглушка:
        _persons =
        [
            new PersonOption(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Іваненко Іван — стрілець"),
            new PersonOption(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Петренко Петро — водій"),
            new PersonOption(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Сидоренко Сидір — оператор"),
        ];

        if (!string.IsNullOrWhiteSpace(_search))
            _persons = [.. _persons.Where(p => p.Display.Contains(_search, StringComparison.OrdinalIgnoreCase))];

        await Task.CompletedTask;
    }

    private void Toggle(Guid id, bool value)
    {
        if (value) _selected.Add(id);
        else _selected.Remove(id);
    }

    private async Task Save()
    {
        await OnSaved.InvokeAsync(new ReplaceGroupPersonsModel(
            GroupId: GroupId,
            PersonIds: [.. _selected]
        ));

        await Close();
    }

    private async Task Close()
        => await IsOpenChanged.InvokeAsync(false);
}