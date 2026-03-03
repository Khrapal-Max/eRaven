//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DocumentsShell
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTasks;

public partial class DocumentsShell : ComponentBase
{
    //======================
    // DI
    //======================
    [Inject] public IQueryHandler<GetCombatTaskDocumentsQuery, IReadOnlyList<CombatTaskDocumentDto>> GetCombatTaskDocumentsQueryHandler { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    //======================
    // UI
    //======================
    private string? _search;
    private bool _loading;
    private bool _createDraftOpen;

    private string _statusRaw = "";
    private DocumentStatusDto? _status;

    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;
    private string _monthIso = DateTime.Today.ToString("yyyy-MM");

    private CombatTaskDocumentDto? _selected;
    private IReadOnlyList<CombatTaskDocumentDto>? _documents;

    //======================
    // Lifecycle
    //======================
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;

        try
        {
            _documents = await GetCombatTaskDocumentsQueryHandler.HandleAsync(new GetCombatTaskDocumentsQuery(
                Year: _year,
                Month: _month,
                Status: _status,
                Search: string.IsNullOrWhiteSpace(_search) ? null : _search.Trim()
            ));
        }
        catch (Exception ex)
        {
            _documents = [];
            ToastService.Error($"Помилка {ex.Message}");
        }
        finally
        {
            _loading = false;
        }
    }

    //======================
    // Commands
    //======================
    private void OpenCreateDraft()
        => _createDraftOpen = true;

    private async Task HandleDraftCreated(Guid documentId)
    {
        // Закриваємо drawer одразу, якщо раптом не закрився
        _createDraftOpen = false;

        // Опційно: підвантажити список (щоб документ зʼявився одразу при Back)
        await ReloadAsync();

        // Переходимо в редактор чернетки
        NavigationManager.NavigateTo($"/task-document/{documentId}");
    }

    //======================
    // Helpers
    //======================
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
        else if (Enum.TryParse<DocumentStatusDto>(_statusRaw, out var st))
            _status = st;

        await ReloadAsync();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _search = Convert.ToString(e.Value);
        if (string.IsNullOrWhiteSpace(_search) || _search.Trim().Length >= 2)
            await ReloadAsync();
    }

    private static string StatusText(DocumentStatusDto s)
        => s switch
        {
            DocumentStatusDto.Active => "Діючий",
            DocumentStatusDto.Canceled => "Відмінений",
            _ => "—"
        };
}
