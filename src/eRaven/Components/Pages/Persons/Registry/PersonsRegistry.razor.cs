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
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    [Inject] public ICommandHandler<CreateReservedCommand, Guid> CreateReservedHandler { get; set; } = default!;
    [Inject] public IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>> PersonsPageQuery { get; set; } = default!;
    [Inject] public ICommandHandler<EnrollCommand, Guid> EnrollHandler { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;

    private int _page = 1;
    private int _pageSize = 6;
    private string? _search;

    private PagedResult<PersonListItemDto> _pageData = new([], 1, 25, 0);
    private PersonListItemDto? _selected;

    private bool _createReservedOpen;

    // NEW: enroll drawer state
    private bool _enrollOpen;
    private Guid _enrollPersonId;

    private int TotalPages =>
    _pageData.TotalCount <= 0 ? 1 : (int)Math.Ceiling(_pageData.TotalCount / (double)_pageSize);

    private bool IsPrevDisabled => _loading || _page <= 1;
    private bool IsNextDisabled => _loading || _page >= TotalPages;

    protected override async Task OnInitializedAsync()
        => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            _pageData = await PersonsPageQuery.HandleAsync(new GetPersonsPageQuery(
                Page: _page,
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

    // NEW: open enroll drawer from table action
    private Task OpenEnroll(PersonListItemDto row)
    {
        _selected = row;
        _enrollPersonId = row.Id;
        _enrollOpen = true;
        return Task.CompletedTask;
    }

    // NEW: placeholder for exclude drawer/command later
    private Task OpenExclude(PersonListItemDto row)
    {
        _selected = row;
        // тут згодом буде ExcludeDrawer
        return Task.CompletedTask;
    }

    private async Task OnEnrolledAsync(Guid personId)
    {
        // щоб таблиця оновила статус/дати
        await ReloadAsync();

        // опційно: перевиставити Selected на той самий запис (якщо він на сторінці)
        _selected = _pageData.Items.FirstOrDefault(x => x.Id == personId) ?? _selected;
        await InvokeAsync(StateHasChanged);
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
             Author: "system", // TODO : додати авторизацію користувачів
             NowUtc: DateTime.UtcNow);

        await CreateReservedHandler.HandleAsync(cmd);

        // (не обовʼязково, але логічно) після створення — оновити список
        await ReloadAsync();
    }

    private async Task HandleEnrollSubmitAsync(EnrollDto enroll)
    {
        var author = "system"; // TODO: User.Identity.Name
        var nowUtc = DateTime.UtcNow;

        try
        {
            await EnrollHandler.HandleAsync(new EnrollCommand(
                PersonId: enroll.Id,
                Kind: enroll.Kind,
                Reference: enroll.Reference,
                Reason: enroll.Reason,
                EnrollDate: enroll.EnrollDate,
                Rank: enroll.Rank,
                Position: enroll.Position,
                Author: author,
                NowUtc: nowUtc));

            Toasts.Success("Зараховано в табель");
            await ReloadAsync();
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
        }
    }

    private async Task PrevPage()
    {
        if (IsPrevDisabled) return;
        _page--;
        await ReloadAsync();
    }

    private async Task NextPage()
    {
        if (IsNextDisabled) return;
        _page++;
        await ReloadAsync();
    }
}