//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDocumentsShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class PlanningDocumentsShell
{
    [Inject] public IQueryHandler<GetPlanningDocumentsQuery, IReadOnlyList<PlanningDocumentRowDto>> Query { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;
    private string? _error;


    private bool _createDraftOpen;

    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;
    private string _monthIso = DateTime.Today.ToString("yyyy-MM");

    private CombatTaskPlanDocumentStatus? _status;
    private string _statusRaw = "";

    private string? _search;

    private IReadOnlyList<PlanningDocumentRowDto>? _rows;
    private PlanningDocumentRowDto? _selected;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            _rows = await Query.HandleAsync(new GetPlanningDocumentsQuery(
                Year: _year,
                Month: _month,
                Status: _status,
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
        if (s.Length >= 7
            && int.TryParse(s[..4], out var y)
            && int.TryParse(s.AsSpan(5, 2), out var m)
            && m is >= 1 and <= 12)
        {
            _year = y;
            _month = m;
            _monthIso = $"{_year:0000}-{_month:00}";
            await ReloadAsync();
        }
    }

    private async Task OnStatusChanged(ChangeEventArgs e)
    {
        _statusRaw = Convert.ToString(e.Value) ?? "";

        if (string.IsNullOrWhiteSpace(_statusRaw))
            _status = null;
        else if (Enum.TryParse<CombatTaskPlanDocumentStatus>(_statusRaw, out var st))
            _status = st;

        await ReloadAsync();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);
        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private void OpenCreateDraft()
        => _createDraftOpen = true;

    private async Task HandleDraftCreated(Guid documentId)
    {
        // Закриваємо drawer одразу, якщо раптом не закрився
        _createDraftOpen = false;

        // Опційно: підвантажити список (щоб документ зʼявився одразу при Back)
        await ReloadAsync();

        // Переходимо в редактор чернетки
        Nav.NavigateTo($"/planning-documents/{documentId}");
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
