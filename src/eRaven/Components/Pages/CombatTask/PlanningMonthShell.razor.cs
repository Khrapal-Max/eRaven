//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningMonthShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class PlanningMonthShell
{
    [Inject] public IQueryHandler<GetPlanningMonthQuery, IReadOnlyList<PlanningMonthAssignmentRowDto>> Query { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;
    private string? _error;

    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;
    private string _monthIso = DateTime.Today.ToString("yyyy-MM");

    private string? _search;

    private IReadOnlyList<PlanningMonthAssignmentRowDto>? _rows;
    private PlanningMonthAssignmentRowDto? _selected;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _rows = await Query.HandleAsync(new GetPlanningMonthQuery(
                Year: _year,
                Month: _month,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));
        }
        catch (Exception ex)
        {
            _rows = [];
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnMonthChanged(ChangeEventArgs e)
    {
        var s = Convert.ToString(e.Value) ?? "";
        // expected yyyy-MM
        if (s.Length >= 7
            && int.TryParse(s[..4], out var y)
            && int.TryParse(s.Substring(5, 2), out var m)
            && m >= 1 && m <= 12)
        {
            _year = y;
            _month = m;
            _monthIso = $"{_year:0000}-{_month:00}";
            await ReloadAsync();
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);

        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private static string StatusText(CombatTaskPlanDocumentStatus s)
        => s switch
        {
            CombatTaskPlanDocumentStatus.Draft => "Чернетка",
            CombatTaskPlanDocumentStatus.Posted => "Проведений",
            CombatTaskPlanDocumentStatus.Canceled => "Відмінений",
            _ => "—"
        };
}
