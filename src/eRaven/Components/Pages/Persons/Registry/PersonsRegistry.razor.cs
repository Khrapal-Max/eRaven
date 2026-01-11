//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public ICommandHandler<CreatePersonCandidateCommand, Guid> CreateCandidate { get; set; } = default!;
    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonTableDto>> PersonsQuery { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private List<PersonTableDto> _items = [];
    private bool _loading;
    private bool _createOpen;

    private PersonTableDto? _selected;

    private string? _search;

    private int _page = 1;
    private int _pageSize = 8;
    private int _totalCount = 0;

    private int CurrentPage => _page;
    private bool HasPrev => _page > 1;
    private bool HasNext => _page * _pageSize < _totalCount;

    protected override async Task OnInitializedAsync()
        => await ReloadAsync(resetPage: true);

    private Task OpenCreateCandidate()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreateCandidate(CreateCandidateDto dto)
    {
        var id = await CreateCandidate.HandleAsync(new CreatePersonCandidateCommand(
            Rnokpp: dto.Rnokpp,
            LastName: dto.LastName,
            FirstName: dto.FirstName,
            MiddleName: dto.MiddleName,
            PlannedPositionUnitId: dto.PlannedPositionUnitId,
            PlannedPosition: dto.PlannedPosition
        ));

        // якщо сортування "нові зверху" — логічно йти на 1 сторінку
        await ReloadAsync(resetPage: true);

        _selected = _items.FirstOrDefault(x => x.Id == id);
    }

    private async Task ReloadAsync(bool resetPage = false)
    {
        if (resetPage) _page = 1;

        _loading = true;
        StateHasChanged();

        try
        {
            var result = await PersonsQuery.HandleAsync(new GetPersonsPageQuery(
                Page: _page,
                PageSize: _pageSize,
                Search: _search
            // AsOfDate/Lifecycle/EnrollmentKind підключиш пізніше
            ));

            _items = [.. result.Items];
            _totalCount = result.TotalCount;

            // якщо вибраний зник після фільтрів/пагінації — скинути
            if (_selected is not null && !_items.Any(x => x.Id == _selected.Id))
                _selected = null;
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task PrevPage()
    {
        if (!HasPrev) return;
        _page--;
        await ReloadAsync();
    }

    private async Task NextPage()
    {
        if (!HasNext) return;
        _page++;
        await ReloadAsync();
    }

    private Task OnSelectedChanged(PersonTableDto? row)
    {
        _selected = row;
        return Task.CompletedTask;
    }

    private Task OnRowClick(PersonTableDto row)
    {
        // TODO: навігація/панель деталей
        return Task.CompletedTask;
    }

    private void OpenCard(PersonTableDto p) => NavigationManager.NavigateTo($"/persons/{p.Id}");
}