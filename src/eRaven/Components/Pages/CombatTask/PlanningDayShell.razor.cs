//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDayShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class PlanningDayShell
{
    [Inject] public IQueryHandler<GetPlanningDayQuery, IReadOnlyList<PlanningDayGroupDto>> Query { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;
    private string? _error;

    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private string _dateIso = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
    private string? _search;

    private IReadOnlyList<PlanningDayGroupDto>? _groups;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _groups = await Query.HandleAsync(new GetPlanningDayQuery(
                Date: _date,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));
        }
        catch (Exception ex)
        {
            _groups = [];
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnDateChanged(ChangeEventArgs e)
    {
        var s = Convert.ToString(e.Value) ?? "";
        if (DateOnly.TryParse(s, out var d))
        {
            _date = d;
            _dateIso = d.ToString("yyyy-MM-dd");
            await ReloadAsync();
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);
        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }
}
