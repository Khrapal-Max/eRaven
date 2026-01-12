//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public ICommandHandler<CreateReservedCommand, Guid> CreateReservedHandler { get; set; } = default!;
    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>> PersonsPageQuery { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;

    private int _pageIndex = 1;
    private int _pageSize = 8;
    private string? _search;

    private PagedResult<PersonListItemDto> _pageData = new([], 1, 8, 0);
    private PersonListItemDto? _selected;

    private bool _createReservedOpen;

    protected override async Task OnInitializedAsync()
        => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            _pageData = await PersonsPageQuery.HandleAsync(new GetPersonsPageQuery(
                Page: _pageIndex,
                PageSize: _pageSize,
                Search: _search
            ));
        }
        finally
        {
            _loading = false;
        }
    }

    private Task OnRowClick(PersonListItemDto row)
    {
        _selected = row;
        return Task.CompletedTask;
    }

    private Task OpenCard(PersonListItemDto row)
    {
        _selected = row;
        // Nav.NavigateTo($"/persons/{row.Id}");
        return Task.CompletedTask;
    }

    private Task OpenCreateReserved()
    {
        _createReservedOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreateReservedAsync(CreateReservedDto dto)
    {
        var cmd = new CreateReservedCommand(
            PersonId: Guid.NewGuid(),
            Rnokpp: dto.Rnokpp,
            LastName: dto.LastName,
            FirstName: dto.FirstName,
            MiddleName: dto.MiddleName,
            Rank: dto.Rank,
            Position: dto.Position,
            Bzvp: dto.Bzvp,
            Weapon: dto.Weapon,
            Callsign: dto.Callsign,
            Author: "system", // TODO: User.Identity.Name
            NowUtc: DateTime.UtcNow);

        await CreateReservedHandler.HandleAsync(cmd);

        // важливо: список має оновитись
        await ReloadAsync();
    }
}